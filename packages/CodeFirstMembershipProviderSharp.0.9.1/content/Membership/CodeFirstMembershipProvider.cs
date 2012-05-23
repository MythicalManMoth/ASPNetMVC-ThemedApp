﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Security;

namespace CodeFirstMembershipSharp
{
    public class CodeFirstMembershipProvider : MembershipProvider
    {

        #region Properties

        public override string ApplicationName
        {
            get
            {
                return this.GetType().Assembly.GetName().Name.ToString();
            }
            set
            {
                this.ApplicationName = this.GetType().Assembly.GetName().Name.ToString();
            }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return 5; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return 0; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return 6; }
        }

        public override int PasswordAttemptWindow
        {
            get { return 0; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return MembershipPasswordFormat.Hashed; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return String.Empty; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return true; }
        }

        #endregion

        #region Functions

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            if (string.IsNullOrEmpty(username))
            {
                status = MembershipCreateStatus.InvalidUserName;
                return null;
            }
            if (string.IsNullOrEmpty(password))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }
            if (string.IsNullOrEmpty(email))
            {
                status = MembershipCreateStatus.InvalidEmail;
                return null;
            }

            string HashedPassword = Crypto.HashPassword(password);
            if (HashedPassword.Length > 128)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            using (DataContext Context = new DataContext())
            {
                if (Context.Users.Where(Usr => Usr.Username == username).Any())
                {
                    status = MembershipCreateStatus.DuplicateUserName;
                    return null;
                }

                if (Context.Users.Where(Usr => Usr.Email == email).Any())
                {
                    status = MembershipCreateStatus.DuplicateEmail;
                    return null;
                }

                User NewUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Username = username,
                    Password = HashedPassword,
                    IsApproved = isApproved,
                    Email = email,
                    CreateDate = DateTime.UtcNow,
                    LastPasswordChangedDate = DateTime.UtcNow,
                    PasswordFailuresSinceLastSuccess = 0,
                    LastLoginDate = DateTime.UtcNow,
                    LastActivityDate = DateTime.UtcNow,
                    LastLockoutDate = DateTime.UtcNow,
                    IsLockedOut = false,
                    LastPasswordFailureDate = DateTime.UtcNow
                };

                Context.Users.Add(NewUser);
                Context.SaveChanges();
                status = MembershipCreateStatus.Success;
                return new MembershipUser(Membership.Provider.Name, NewUser.Username, NewUser.UserId, NewUser.Email, null, null, NewUser.IsApproved, NewUser.IsLockedOut, NewUser.CreateDate.Value, NewUser.LastLoginDate.Value, NewUser.LastActivityDate.Value, NewUser.LastPasswordChangedDate.Value, NewUser.LastLockoutDate.Value);
            }
        }

        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }
            using (DataContext Context = new DataContext())
            {
                User User = null;
                User = Context.Users.FirstOrDefault(Usr => Usr.Username == username);
                if (User == null)
                {
                    return false;
                }
                if (!User.IsApproved)
                {
                    return false;
                }
                if (User.IsLockedOut)
                {
                    return false;
                }
                String HashedPassword = User.Password;
                Boolean VerificationSucceeded = (HashedPassword != null && Crypto.VerifyHashedPassword(HashedPassword, password));
                if (VerificationSucceeded)
                {
                    User.PasswordFailuresSinceLastSuccess = 0;
                    User.LastLoginDate = DateTime.UtcNow;
                    User.LastActivityDate = DateTime.UtcNow;
                }
                else
                {
                    int Failures = User.PasswordFailuresSinceLastSuccess;
                    if (Failures < MaxInvalidPasswordAttempts)
                    {
                        User.PasswordFailuresSinceLastSuccess += 1;
                        User.LastPasswordFailureDate = DateTime.UtcNow;
                    }
                    else if (Failures >= MaxInvalidPasswordAttempts)
                    {
                        User.LastPasswordFailureDate = DateTime.UtcNow;
                        User.LastLockoutDate = DateTime.UtcNow;
                        User.IsLockedOut = true;
                    }
                }
                Context.SaveChanges();
                if (VerificationSucceeded)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }
            using (DataContext Context = new DataContext())
            {
                User User = null;
                User = Context.Users.FirstOrDefault(Usr => Usr.Username == username);
                if (User != null)
                {
                    if (userIsOnline)
                    {
                        User.LastActivityDate = DateTime.UtcNow;
                        Context.SaveChanges();
                    }
                    return new MembershipUser(Membership.Provider.Name, User.Username, User.UserId, User.Email, null, null, User.IsApproved, User.IsLockedOut, User.CreateDate.Value, User.LastLoginDate.Value, User.LastActivityDate.Value, User.LastPasswordChangedDate.Value, User.LastLockoutDate.Value);
                }
                else
                {
                    return null;
                }
            }
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (providerUserKey is Guid) { }
            else
            {
                return null;
            }

            using (DataContext Context = new DataContext())
            {
                User User = null;
                User = Context.Users.Find(providerUserKey);
                if (User != null)
                {
                    if (userIsOnline)
                    {
                        User.LastActivityDate = DateTime.UtcNow;
                        Context.SaveChanges();
                    }
                    return new MembershipUser(Membership.Provider.Name, User.Username, User.UserId, User.Email, null, null, User.IsApproved, User.IsLockedOut, User.CreateDate.Value, User.LastLoginDate.Value, User.LastActivityDate.Value, User.LastPasswordChangedDate.Value, User.LastLockoutDate.Value);
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }
            if (string.IsNullOrEmpty(oldPassword))
            {
                return false;
            }
            if (string.IsNullOrEmpty(newPassword))
            {
                return false;
            }
            using (DataContext Context = new DataContext())
            {
                User User = null;
                User = Context.Users.FirstOrDefault(Usr => Usr.Username == username);
                if (User == null)
                {
                    return false;
                }
                String HashedPassword = User.Password;
                Boolean VerificationSucceeded = (HashedPassword != null && Crypto.VerifyHashedPassword(HashedPassword, oldPassword));
                if (VerificationSucceeded)
                {
                    User.PasswordFailuresSinceLastSuccess = 0;
                }
                else
                {
                    int Failures = User.PasswordFailuresSinceLastSuccess;
                    if (Failures < MaxInvalidPasswordAttempts)
                    {
                        User.PasswordFailuresSinceLastSuccess += 1;
                        User.LastPasswordFailureDate = DateTime.UtcNow;
                    }
                    else if (Failures >= MaxInvalidPasswordAttempts)
                    {
                        User.LastPasswordFailureDate = DateTime.UtcNow;
                        User.LastLockoutDate = DateTime.UtcNow;
                        User.IsLockedOut = true;
                    }
                    Context.SaveChanges();
                    return false;
                }
                String NewHashedPassword = Crypto.HashPassword(newPassword);
                if (NewHashedPassword.Length > 128)
                {
                    return false;
                }
                User.Password = NewHashedPassword;
                User.LastPasswordChangedDate = DateTime.UtcNow;
                Context.SaveChanges();
                return true;
            }
        }

        public override bool UnlockUser(string userName)
        {
            using (DataContext Context = new DataContext())
            {
                User User = null;
                User = Context.Users.FirstOrDefault(Usr => Usr.Username == userName);
                if (User != null)
                {
                    User.IsLockedOut = false;
                    User.PasswordFailuresSinceLastSuccess = 0;
                    Context.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override int GetNumberOfUsersOnline()
        {
            DateTime DateActive = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(Convert.ToDouble(Membership.UserIsOnlineTimeWindow)));
            using (DataContext Context = new DataContext())
            {
                return Context.Users.Where(Usr => Usr.LastActivityDate > DateActive).Count();
            }
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }
            using (DataContext Context = new DataContext())
            {
                User User = null;
                User = Context.Users.FirstOrDefault(Usr => Usr.Username == username);
                if (User != null)
                {
                    Context.Users.Remove(User);
                    Context.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override string GetUserNameByEmail(string email)
        {
            using (DataContext Context = new DataContext())
            {
                User User = null;
                User = Context.Users.FirstOrDefault(Usr => Usr.Email == email);
                if (User != null)
                {
                    return User.Username;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection MembershipUsers = new MembershipUserCollection();
            using (DataContext Context = new DataContext())
            {
                totalRecords = Context.Users.Where(Usr => Usr.Email == emailToMatch).Count();
                IQueryable<User> Users = Context.Users.Where(Usr => Usr.Email == emailToMatch).OrderBy(Usrn => Usrn.Username).Skip(pageIndex * pageSize).Take(pageSize);
                foreach (User user in Users)
                {
                    MembershipUsers.Add(new MembershipUser(Membership.Provider.Name, user.Username, user.UserId, user.Email, null, null, user.IsApproved, user.IsLockedOut, user.CreateDate.Value, user.LastLoginDate.Value, user.LastActivityDate.Value, user.LastPasswordChangedDate.Value, user.LastLockoutDate.Value));
                }
            }
            return MembershipUsers;
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection MembershipUsers = new MembershipUserCollection();
            using (DataContext Context = new DataContext())
            {
                totalRecords = Context.Users.Where(Usr => Usr.Username == usernameToMatch).Count();
                IQueryable<User> Users = Context.Users.Where(Usr => Usr.Username == usernameToMatch).OrderBy(Usrn => Usrn.Username).Skip(pageIndex * pageSize).Take(pageSize);
                foreach (User user in Users)
                {
                    MembershipUsers.Add(new MembershipUser(Membership.Provider.Name, user.Username, user.UserId, user.Email, null, null, user.IsApproved, user.IsLockedOut, user.CreateDate.Value, user.LastLoginDate.Value, user.LastActivityDate.Value, user.LastPasswordChangedDate.Value, user.LastLockoutDate.Value));
                }
            }
            return MembershipUsers;
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            MembershipUserCollection MembershipUsers = new MembershipUserCollection();
            using (DataContext Context = new DataContext())
            {
                totalRecords = Context.Users.Count();
                IQueryable<User> Users = Context.Users.OrderBy(Usrn => Usrn.Username).Skip(pageIndex * pageSize).Take(pageSize);
                foreach (User user in Users)
                {
                    MembershipUsers.Add(new MembershipUser(Membership.Provider.Name, user.Username, user.UserId, user.Email, null, null, user.IsApproved, user.IsLockedOut, user.CreateDate.Value, user.LastLoginDate.Value, user.LastActivityDate.Value, user.LastPasswordChangedDate.Value, user.LastLockoutDate.Value));
                }
            }
            return MembershipUsers;
        }

        #endregion

        #region Not Supported

        //CodeFirstMembershipProvider does not support password retrieval scenarios.
        public override bool EnablePasswordRetrieval
        {
            get { return false; }
        }
        public override string GetPassword(string username, string answer)
        {
            throw new NotSupportedException("Consider using methods from WebSecurity module.");
        }

        //CodeFirstMembershipProvider does not support password reset scenarios.
        public override bool EnablePasswordReset
        {
            get { return false; }
        }
        public override string ResetPassword(string username, string answer)
        {
            throw new NotSupportedException("Consider using methods from WebSecurity module.");
        }

        //CodeFirstMembershipProvider does not support question and answer scenarios.
        public override bool RequiresQuestionAndAnswer
        {
            get { return false; }
        }
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotSupportedException("Consider using methods from WebSecurity module.");
        }

        //CodeFirstMembershipProvider does not support UpdateUser because this method is useless.
        public override void UpdateUser(MembershipUser user)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}