using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using GeoStoreAuthApplication.Models;
using DotNetOpenAuth.OpenId.RelyingParty;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.Messaging;
using Newtonsoft.Json;

namespace GeoStoreAuthApplication.Controllers
{
    public class AccountController : Controller
    {

        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                MembershipCreateStatus createStatus;
                Membership.CreateUser(model.UserName, model.Password, model.Email, null, null, true, null, out createStatus);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, false /* createPersistentCookie */);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", ErrorCodeToString(createStatus));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePassword

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {

                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    MembershipUser currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
                    changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }

        #region Status Codes
        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion

        
        //OpenId implementation.

        //This class is used to send verification messages.
        class VerificationResponse
        {
            public int status;
            public string message;
        }

        private static OpenIdRelyingParty openid = new OpenIdRelyingParty();

        [ValidateInput(false)]
        [HttpGet]
        public ActionResult LogOnOpenId(){
            return View();
        }
        
        /// <summary>
        /* Authentication/Login post action.
         * Original code concept from DotNetOpenAuth/Samples/OpenIdRelyingPartyMvc/Controller/UserController.
         * This function is used to authenticate user through open id. It acts both as HttpPost and HttpGet. HttpPost is occured
         * when a form posts values to this function through RegisterOpenIdModel parameter. The second call HttpGet is made by 
         * DotNetOpenAuth library. This library calls this function as a call back when this library receives a response from the
         * selected open id provider.
         */
        [ValidateInput(false)]
        public ActionResult AuthenticateOpenId(RegisterOpenIdModel model)
        {
            //Get response from the open id provider. When HttpPost call is made through form this value does not have any open id
            //provider. Therefore the return value is null. When this function is called as HttpGet by DotNetOpenAuth library then 
            // it returns response from the open id provider.
            var response = openid.GetResponse();
            var statusMessage = "";

            //first time this call is for post and response is null.
            if (response == null)
            {
                //save data in session.
                saveUserInSession(model);

                Identifier id;
                //make sure that the url of open id provider is valid.
                if (Identifier.TryParse(model.openid_identifier, out id))
                {
                    try
                    {
                        //Request open id provider to authenticate user. DotNetOpenAuth acts as a relying party 
                        //so it waits for the response from the open id provider. When response is recieved from the open id provider
                        //DotNetOpenAuth calls this function again using HttpGet.
                        return openid.CreateRequest(model.openid_identifier).RedirectingResponse.AsActionResult();
                    }
                    catch (ProtocolException ex)
                    {
                        statusMessage = ex.Message;
                        ModelState.AddModelError("openid_identifier", statusMessage);
                        return View("RegisterOpenId", model);
                    }
                }
                else
                {
                    statusMessage = "Open id identifier url is invalid. Please check if you have typed correct url.";
                    ModelState.AddModelError("openid_identifier", statusMessage);
                    return View("RegisterOpenId", model);
                }
            }
            //This is executed when this function is called as HttpGet from DotNetOpenAuth library. DotNetOpenAuth calls this
            //when it receives a response from the open id provider.
            else
            {
                //retrieve user from session.
                user userObj = retrieveUserFromSession();
                model.UserName = userObj.name;
                model.Email = userObj.email;
                model.openid_identifier = userObj.open_id;

                //check the response status
                switch (response.Status)
                {
                    //success status.
                    case AuthenticationStatus.Authenticated:
                        //Check if this id is already registered in the database.
                        if (VerifyOpenId(response.ClaimedIdentifier).status == 1)
                        {
                            //if user is not register then register this user into the database.
                            userObj.open_id = response.ClaimedIdentifier;
                            saveUserIndb(userObj);
                            Session["FriendlyIdentifier"] = response.FriendlyIdentifierForDisplay;
                            FormsAuthentication.SetAuthCookie(response.ClaimedIdentifier, true);
                            string message = "Thank you " + Session["UserName"] + ". You are now registered with the Geostore.";
                            TempData["message"] = message;
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError("openid_identifier", "You are already registered with this identifier.");
                            return View("RegisterOpenId", model);
                        }

                    case AuthenticationStatus.Canceled:
                        ModelState.AddModelError("openid_identifier", "Open identifier authentication has been cancelled at open id provider.");
                        return View("RegisterOpenId", model);

                    case AuthenticationStatus.Failed:
                        ModelState.AddModelError("openid_identifier", "Open identifier authentication has failed at open id provider.");
                        ModelState.AddModelError("openid_identifier", response.Exception.Message);
                        return View("RegisterOpenId", model);
                }
            }
            return new EmptyResult();
        }

        //store user registration data in session.
        private void saveUserInSession(RegisterOpenIdModel model)
        {
            Session["UserName"] = model.UserName;
            Session["Email"] = model.Email;
            Session["open_identifier"] = model.openid_identifier;
        }

        private user saveUserIndb(user userObj)
        {
            return user.createUser(userObj);
        }

        //retrieve user data from session.
        private user retrieveUserFromSession()
        {
            user userObj = new user();
            userObj.name = Session["UserName"].ToString();
            userObj.email = Session["Email"].ToString();
            userObj.open_id = Session["open_identifier"].ToString();
            return userObj;
        }
        
        //
        // GET: /Account/RegisterOpenId
        [HttpGet]
        public ActionResult RegisterOpenId()
        {
            return View();
        }


        //Get: /Account/VerifyUser
        //This function is used to check if a user with the input name exists in the database.
        [HttpGet]
        public string VerifyUser(string userName)
        {
            VerificationResponse verificationResponse = new VerificationResponse();
            GeoStoreDBEntities db = new GeoStoreDBEntities();
            user userObj = user.getUserByName(userName);

            if (userObj != null)
            {
                verificationResponse.status = 0;
                verificationResponse.message = "This user name is not available. Please choose another name.";
            }
            else
            {
                verificationResponse.status = 1;
                verificationResponse.message = "This User name is available.";
            }
            return JsonConvert.SerializeObject(verificationResponse);
        }

        //Get: /Account/VerifyEmailAddress
        //This function is used to check if a user with the input email address exists in the database.
        [HttpGet]
        public string VerifyEmailAddress(string email)
        {
            VerificationResponse verificationResponse = new VerificationResponse();
            GeoStoreDBEntities db = new GeoStoreDBEntities();
            var userObj = user.getUserByEmail(email);

            if (userObj != null)
            {
                verificationResponse.status = 0;
                verificationResponse.message = "This email is already registered. You might have already registered with us.";
            }
            else
            {
                verificationResponse.status = 1;
                verificationResponse.message = "";
            }
            return JsonConvert.SerializeObject(verificationResponse);
        }

        //check if open id exists in our system.
        private VerificationResponse VerifyOpenId(string openid)
        {
            VerificationResponse verificationResponse = new VerificationResponse();
            GeoStoreDBEntities db = new GeoStoreDBEntities();
            user userObj = user.getUserByOpenID(openid);

            if (userObj != null)
            {
                verificationResponse.status = 0;
                verificationResponse.message = "This openid is already registered. You might have already registered with us.";
            }
            else
            {
                verificationResponse.status = 1;
                verificationResponse.message = "The openid is successfully authenticated.";
            }
            return verificationResponse;
        }        
    }
}
