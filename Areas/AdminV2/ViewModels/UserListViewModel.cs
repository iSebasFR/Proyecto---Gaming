namespace Proyecto_Gaming.Areas.AdminV2.ViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "Usuario";
        public bool IsActive { get; set; }
    }
}
