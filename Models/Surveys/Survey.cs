using System;
using System.Collections.Generic;

namespace Proyecto_Gaming.Models.Surveys
{
    public class Survey
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
        public string? Description { get; set; }

        // Fechas en UTC siempre
        public DateTime StartDate { get; set; }        // Obligatoria
        public DateTime? EndDate { get; set; }         // Opcional (null = sin fecha fin)

        // Relación con Medalla (recompensa)
        public int? MedalId { get; set; }
        public Medal? Medal { get; set; }

        // Preguntas asociadas
        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();

        // Respuestas de usuarios
        public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
    }
}
