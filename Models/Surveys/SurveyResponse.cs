using System;
using System.Collections.Generic;
using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Models.Surveys
{
    public class SurveyResponse
    {
        public int Id { get; set; }

        public int SurveyId { get; set; }
        public Survey? Survey { get; set; }

        public string UsuarioId { get; set; } = default!;
        public Usuario? Usuario { get; set; }

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
    }
}
