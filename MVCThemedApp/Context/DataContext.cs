using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using $safeprojectname$.Models;

namespace $safeprojectname$
{
    public class DataContext : DbContext
    {   
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
    }
}
