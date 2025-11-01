using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Areas.AdminV2.ViewModels;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models.Surveys;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Proyecto_Gaming.Areas.AdminV2.Controllers
{
    [Area("AdminV2")]
    public class SurveysController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SurveysController(ApplicationDbContext db) => _db = db;

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

        public async Task<IActionResult> Create()
        {
            var vm = new SurveyCreateViewModel
            {
                Medals = await _db.Medals
                    .Select(m => new SurveyCreateViewModel.MedalItemVM { Id = m.Id, Name = m.Name })
                    .ToListAsync(),
                Questions = new List<SurveyCreateViewModel.QuestionVM>
                {
                    new SurveyCreateViewModel.QuestionVM
                    {
                        Type = QuestionType.MultipleChoice,
                        Options = new List<SurveyCreateViewModel.OptionVM>
                        {
                            new() { Text = "" }, new() { Text = "" }
                        }
                    }
                }
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SurveyCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Medals = await _db.Medals
                    .Select(m => new SurveyCreateViewModel.MedalItemVM { Id = m.Id, Name = m.Name })
                    .ToListAsync();
                return View(vm);
            }

            if (vm.EndDateUtc <= vm.StartDateUtc)
            {
                ModelState.AddModelError(nameof(vm.EndDateUtc), "La fecha de fin debe ser posterior al inicio.");
                vm.Medals = await _db.Medals
                    .Select(m => new SurveyCreateViewModel.MedalItemVM { Id = m.Id, Name = m.Name })
                    .ToListAsync();
                return View(vm);
            }

            var survey = new Survey
            {
                Title = vm.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                StartDateUtc = vm.StartDateUtc,
                EndDateUtc = vm.EndDateUtc,
                MedalId = vm.MedalId
            };

            int qOrder = 0;
            foreach (var q in vm.Questions.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
            {
                var question = new SurveyQuestion
                {
                    Text = q.Text.Trim(),
                    Type = q.Type,
                    Order = qOrder++
                };

                if (q.Type == QuestionType.MultipleChoice)
                {
                    int oOrder = 0;
                    foreach (var opt in q.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                    {
                        question.Options.Add(new SurveyOption
                        {
                            Text = opt.Text.Trim(),
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
