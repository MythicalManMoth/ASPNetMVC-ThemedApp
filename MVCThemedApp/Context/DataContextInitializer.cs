using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Security;
using $safeprojectname$.Models;

namespace $safeprojectname$
{
    public class DataContextInitializer : DropCreateDatabaseIfModelChanges<DataContext>
    {
        protected override void Seed(DataContext context)
        {
            // Create a new Demo User
            MembershipCreateStatus Status;
            Membership.CreateUser("Demo", "123456", "demo@demo.com", null, null, true, out Status);
            Roles.CreateRole("Admin");
            Roles.AddUserToRole("Demo", "Admin");
        }
    }
}