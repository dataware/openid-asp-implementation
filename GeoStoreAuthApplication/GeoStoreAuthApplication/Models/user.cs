using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Objects.DataClasses;

namespace GeoStoreAuthApplication.Models
{
    public partial class user : EntityObject
    {
        public static user getUserByName(string userName)
        {
            GeoStoreDBEntities db = new GeoStoreDBEntities();
            var userQuery = from userObj in db.users
                            where userObj.name == userName
                            select userObj;
            var users = userQuery.ToList();
            if (users.Count > 0)
                return users.Single();
            else
                return null;
        }

        public static user getUserByEmail(string email)
        {
            GeoStoreDBEntities db = new GeoStoreDBEntities();
            var userQuery = from userObj in db.users
                            where userObj.email == email
                            select userObj;
            var users = userQuery.ToList();
            if (users.Count > 0)
                return users.Single();
            else
                return null;
        }

        public static user getUserByOpenID(string openid)
        {
            GeoStoreDBEntities db = new GeoStoreDBEntities();
            var userQuery = from userObj in db.users
                            where userObj.open_id == openid
                            select userObj;
            var users = userQuery.ToList();
            if (users.Count > 0)
                return users.Single();
            else
                return null;
        }

        public static user createUser(user userObj)
        {
            GeoStoreDBEntities db = new GeoStoreDBEntities();
            db.users.AddObject(userObj);
            db.SaveChanges();
            return (getUserByName(userObj.name));
        }
    }

}
