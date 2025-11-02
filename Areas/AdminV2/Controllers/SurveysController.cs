using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models.Surveys;
// Alias para evitar confusiones con otros nombres iguales
using AdminV2VM = Proyecto_Gaming.Areas.AdminV2.ViewModels.SurveyCreateViewModel;

namespace Proyecto_Gaming.Areas.AdminV2.Controllers
{
    [Area("AdminV2")]
    public class SurveysController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SurveysController(ApplicationDbContext db) => _db = db;

        // GET: /AdminV2/Surveys
        public async Task<IActionResult> Index()
        {
            var list = await _db.Surveys
                .AsNoTracking()
                .Include(s => s.Medal)
                .Include(s => s.Questions)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            return View(list);
        }

        // GET: /AdminV2/Surveys/Create
        public async Task<IActionResult> Create()
        {
            var vm = new AdminV2VM
            {
                Medals = await _db.Medals
                    .Select(m => new AdminV2VM.MedalItemVM { Id = m.Id, Name = m.Name })
                    .ToListAsync(),
                Questions = new List<AdminV2VM.QuestionVM>
                {
                    new AdminV2VM.QuestionVM
                    {
                        Type = QuestionType.MultipleChoice,
                        Options = new List<AdminV2VM.OptionVM>
                        {
                            new() { Text = "" },
                            new() { Text = "" }
                        }
                    }
                }
            };

            return View(vm);
        }

        // POST: /AdminV2/Surveys/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminV2VM vm)
        {
            if (!ModelState.IsValid)
            {
                // recargar medallas para que el formulario vuelva completo
                vm.Medals = await _db.Medals
                    .Select(m => new AdminV2VM.MedalItemVM { Id = m.Id, Name = m.Name })
                    .ToListAsync();
                return View(vm);
            }

            // Validación de rango (fin > inicio)
            if (vm.EndDateUtc <= vm.StartDateUtc)
            {
                ModelState.AddModelError(nameof(vm.EndDateUtc), "La fecha de fin debe ser posterior al inicio.");
                vm.Medals = await _db.Medals
                    .Select(m => new AdminV2VM.MedalItemVM { Id = m.Id, Name = m.Name })
                    .ToListAsync();
                return View(vm);
            }

            // Normalizar explícitamente a UTC (aseguramos Kind=Utc)
            var startUtc = DateTime.SpecifyKind(vm.StartDateUtc, DateTimeKind.Utc);
            var endUtc   = DateTime.SpecifyKind(vm.EndDateUtc,   DateTimeKind.Utc);

            // Si EndDate en la entidad es nullable, usamos DateTime?
            DateTime? endForEntity = endUtc;

            // Crear entidad
            var survey = new Survey
            {
                Title       = vm.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                StartDate   = startUtc,     // DateTime (no nullable)
                EndDate     = endForEntity, // DateTime? en la entidad Survey
                MedalId     = vm.MedalId
            };

            // Construir preguntas
            var qOrder = 0;
            foreach (var q in vm.Questions.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
            {
                var question = new SurveyQuestion
                {
                    Text  = q.Text.Trim(),
                    Type  = q.Type,
                    Order = qOrder++
                };

                if (q.Type == QuestionType.MultipleChoice)
                {
                    var oOrder = 0;
                    foreach (var opt in q.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                    {
                        question.Options.Add(new SurveyOption
                        {
                            Text  = opt.Text.Trim(),
                            Order = oOrder++
                        });
                    }
                }
                else if (q.Type == QuestionType.YesNo)
                {
                    question.Options.Add(new SurveyOption { Text = "Sí", Order = 0 });
                    question.Options.Add(new SurveyOption { Text = "No", Order = 1 });
                }

                survey.Questions.Add(question);
            }

            _db.Surveys.Add(survey);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Encuesta creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
