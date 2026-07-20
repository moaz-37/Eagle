using Microsoft.AspNetCore.Identity;

namespace Eagle.DAL.Entities
{
    public class Role : IdentityRole<Guid>
    {
        public Role() : base() { }

        public Role(string roleName) : base(roleName) { }
    }
}