using System.Collections.Generic;

namespace Proyecto_Gaming.Models.Surveys
{
    public class Medal
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";      // p.e. "Explorador"
        public string? Icon { get; set; }           // clase fa-*, url o svg
        public int Points { get; set; } = 0;

        public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    }
}
