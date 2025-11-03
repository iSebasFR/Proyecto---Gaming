using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.ViewModels.Surveys;   // ✅ Asegura el namespace correcto del VM
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Models.Surveys;

namespace Proyecto_Gaming.Controllers
{
    [Authorize] // solo usuarios logueados
    public class EncuestasController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<Usuario> _userManager;

        public EncuestasController(ApplicationDbContext db, UserManager<Usuario> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private Task<Usuario?> GetUserAsync() => _userManager.GetUserAsync(User);

        // GET: /Encuestas
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            var surveys = await _db.Surveys
                .AsNoTracking()
                .Where(s => s.StartDate <= now && (s.EndDate == null || s.EndDate >= now))
                .Select(s => new SurveyListItemVM
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    MedalName = s.Medal != null ? s.Medal.Name : "Participación"
                })
                .ToListAsync();

            return View(surveys);
        }

        // GET: /Encuestas/Responder/5
        [HttpGet]
        public async Task<IActionResult> Responder(int id)
        {
            var user = await GetUserAsync();
            if (user is null) return Challenge();

            // Evitar doble respuesta
            var yaRespondio = await _db.SurveyResponses
                .AnyAsync(r => r.SurveyId == id && r.UsuarioId == user.Id);
            if (yaRespondio)
            {
                TempData["Error"] = "Ya respondiste esta encuesta.";
                return RedirectToAction(nameof(Index));
            }

            var survey = await _db.Surveys
                .AsNoTracking() // ✅ evita tracking innecesario
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                TempData["Error"] = "La encuesta no existe.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ Usamos SIEMPRE el tipo totalmente calificado para evitar choques con otras clases iguales
            var vm = new Proyecto_Gaming.ViewModels.Surveys.TakeSurveyVM
            {
                SurveyId = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                Questions = survey.Questions
                    .OrderBy(q => q.Id)
                    .Select(q => new Proyecto_Gaming.ViewModels.Surveys.TakeQuestionVM
                    {
                        QuestionId = q.Id,
                        Text = q.Text,
                        Type = q.Type, // enum
                        Options = q.Options
                            .OrderBy(o => o.Id)
                            .Select(o => new Proyecto_Gaming.ViewModels.Surveys.TakeOptionVM
                            {
                                OptionId = o.Id,
                                Text = o.Text
                            }).ToList()
                    }).ToList()
            };

            return View("Responder", vm); // ✅ forzamos la vista correcta
        }

        // POST: /Encuestas/Responder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Responder(Proyecto_Gaming.ViewModels.Surveys.TakeSurveyVM model) // ✅ firma totalmente calificada
        {
            var user = await GetUserAsync();
            if (user is null) return Challenge();

            // Validar encuesta y doble respuesta
            var survey = await _db.Surveys
                .Include(s => s.Questions).ThenInclude(q => q.Options)
                .Include(s => s.Medal)
                .FirstOrDefaultAsync(s => s.Id == model.SurveyId);

            if (survey == null)
            {
                TempData["Error"] = "La encuesta no existe.";
                return RedirectToAction(nameof(Index));
            }

            var yaRespondio = await _db.SurveyResponses
                .AnyAsync(r => r.SurveyId == survey.Id && r.UsuarioId == user.Id);
            if (yaRespondio)
            {
                TempData["Error"] = "Ya respondiste esta encuesta.";
                return RedirectToAction(nameof(Index));
            }

            // Guardar respuesta principal
            var response = new SurveyResponse
            {
                SurveyId = survey.Id,
                UsuarioId = user.Id,
                SubmittedAtUtc = DateTime.UtcNow,
                Answers = new List<SurveyAnswer>()
            };

            // Mapear respuestas por pregunta
            foreach (var q in model.Questions)
            {
                var dbq = survey.Questions.FirstOrDefault(x => x.Id == q.QuestionId);
                if (dbq == null) continue;

                if (dbq.Type == QuestionType.OpenText)
                {
                    if (!string.IsNullOrWhiteSpace(q.OpenAnswer))
                    {
                        response.Answers.Add(new SurveyAnswer
                        {
                            SurveyQuestionId = dbq.Id,
                            AnswerText = q.OpenAnswer
                        });
                    }
                }
                else if (dbq.Type == QuestionType.MultipleChoice || dbq.Type == QuestionType.YesNo)
                {
                    if (q.SelectedOptionId.HasValue &&
                        dbq.Options.Any(o => o.Id == q.SelectedOptionId.Value))
                    {
                        response.Answers.Add(new SurveyAnswer
                        {
                            SurveyQuestionId = dbq.Id,
                            SelectedOptionId = q.SelectedOptionId
                        });
                    }
                }
            }

            _db.SurveyResponses.Add(response);

            // Ajuste UTC defensivo (si el modelo lo necesita)
            survey.StartDate = DateTime.SpecifyKind(survey.StartDate, DateTimeKind.Utc);
            if (survey.EndDate.HasValue)
                survey.EndDate = DateTime.SpecifyKind(survey.EndDate.Value, DateTimeKind.Utc);

            await _db.SaveChangesAsync();

            // Otorgar medalla si la encuesta tiene
            if (survey.MedalId.HasValue)
            {
                var medalId = survey.MedalId.Value;

                var yaTiene = await _db.UserMedals
                    .AnyAsync(m => m.UsuarioId == user.Id && m.MedalId == medalId);

                if (!yaTiene)
                {
                    _db.UserMedals.Add(new UserMedal
                    {
                        UsuarioId = user.Id,
                        MedalId = medalId,
                        GrantedAtUtc = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }
            }

            TempData["Ok"] = "¡Gracias por participar! Se registraron tus respuestas y tu medalla fue otorgada.";
            return RedirectToAction(nameof(Index));
        }
    }
}
