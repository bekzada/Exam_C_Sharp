using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FridayExam.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridayExam.Controllers {
    public class HomeController : Controller {
        private HomeContext dbContext;
        public HomeController (HomeContext context) {
            dbContext = context;
        }

        [HttpGet ("")]
        public IActionResult Index () {
            return View ();
        }

        [HttpPost ("register")]
        public IActionResult Register (User newUser) {
            Regex rx = new Regex (@"\d");
            if (newUser.FirstName != null) {
                MatchCollection FirstNameMatches = rx.Matches (newUser.FirstName);
                if (FirstNameMatches.Count != 0) {
                    ModelState.AddModelError ("FirstName", "'Name' Fields cannot contain numeric characters");
                }
            }
            if (newUser.LastName != null) {
                MatchCollection LastNameMatches = rx.Matches (newUser.LastName);
                if (LastNameMatches.Count != 0) {
                    ModelState.AddModelError ("LastName", "'Name' Fields cannot contain numeric characters");
                }
            }
            if (newUser.Password != null) {
                MatchCollection PasswordNumberMatches = rx.Matches (newUser.Password);
                if (PasswordNumberMatches.Count == 0) {
                    ModelState.AddModelError ("Password", "Password should contain at least one number");
                }
                string SpecialChars = @"\|!#$%&/()=?»«@£§€{}.-;'<>_,";
                bool HasSpecialChars = false;
                foreach (var character in SpecialChars) {
                    if (newUser.Password.Contains (character)) {
                        HasSpecialChars = true;
                    }
                }
                if (!HasSpecialChars) {
                    ModelState.AddModelError ("Password", "Password should contain at least one special character");
                }
            }
            if (dbContext.users.Any (c => c.Email == newUser.Email)) {
                ModelState.AddModelError ("Email", "That Email is taken");
            }
            if (ModelState.IsValid) {
                PasswordHasher<User> Hasher = new PasswordHasher<User> ();
                newUser.Password = Hasher.HashPassword (newUser, newUser.Password);
                HttpContext.Session.SetString ("UserName", newUser.FirstName + " " + newUser.LastName);
                dbContext.users.Add (newUser);
                dbContext.SaveChanges ();
                HttpContext.Session.SetInt32 ("ID", newUser.UserId);
                return Redirect ("sign");
            }
            return View ("Index");
        }
        [HttpGet("sign")]
        public IActionResult Sign ()
        {
            return View();
        }

        [HttpPost("login")]
        public IActionResult Login (Login _logUSer) {
            User DbUser = dbContext.users.FirstOrDefault (c => c.Email == _logUSer.LoginEmail);
            if (DbUser == null) {
                ModelState.AddModelError ("LoginEmail", "Email not found. Register?");
            }
            if (ModelState.IsValid) {
                var hasher = new PasswordHasher<Login> ();
                var result = hasher.VerifyHashedPassword (_logUSer, DbUser.Password, _logUSer.LoginPassword);
                if (result == 0) {
                    ModelState.AddModelError ("LoginEmail", "Email or password not valid");
                    return View ("Index");
                }
                HttpContext.Session.SetInt32 ("ID", DbUser.UserId);
                HttpContext.Session.SetString ("UserName", DbUser.FirstName + " " + DbUser.LastName);
                return Redirect ("home");
            }
            return View ("Index");
        }

        [HttpGet ("logout")]
        public IActionResult LogOut () {
            HttpContext.Session.Clear ();
            return Redirect ("/");
        }

        [HttpGet ("home")]
        public IActionResult Home () {
            if (HttpContext.Session.GetInt32 ("ID") == null) {
                return Redirect ("/");
            }
            List<Club> Clubs = dbContext.Clubs
                .Include (u => u.Participents)
                .ThenInclude (u => u.User)
                .Include (e => e.Coordinator)
                .ToList ();
            ViewBag.Clubs = Clubs;
            int? seshUser = HttpContext.Session.GetInt32 ("ID");

            List<Participent> yourClubs = dbContext.Participants
                .Where (p => p.UserID == (int) seshUser)
                .Include (p => p.Club)
                .ToList ();
            ViewBag.EventsWithConflicts = yourClubs;
            return View ();
        }

        [HttpGet ("newClub")]
        public IActionResult NewClubForm () {
            if (HttpContext.Session.GetInt32 ("ID") == null) {
                return Redirect ("/");
            }
            HttpContext.Session.SetString ("Page", "newForm");
            return View ();
        }

        [HttpPost ("newClub")]
        public IActionResult NewClub (Club newClub) {
            if (newClub.DateOfClub != null) {
                DateTime Now = (DateTime) newClub.DateOfClub;
                if (Now.Date < DateTime.Today.Date) {
                    ModelState.AddModelError ("DateOfClub", "Please input a future date for your Club.");
                }
            }

            if (ModelState.IsValid) {
                int? seshUser = HttpContext.Session.GetInt32 ("ID");
                User ClubCreator = dbContext.users.FirstOrDefault (u => u.UserId == seshUser);
                newClub.Coordinator = ClubCreator;
                newClub.CoordinatorID = (int) seshUser;
                dbContext.Clubs.Add (newClub);
                dbContext.SaveChanges ();
                return Redirect ($"club/{newClub.ID}");
            }
            return View ("NewClubForm");
        }

        [HttpGet ("club/{ClubID}")]
        public IActionResult ViewClub (int ClubID) {
            if (HttpContext.Session.GetInt32 ("ID") == null) {
                return Redirect ("/");
            }
            HttpContext.Session.SetString ("Page", "View");
            Club thisClub = dbContext.Clubs
                .Include (u => u.Participents)
                .ThenInclude (e => e.User)
                .Include (e => e.Coordinator)

                .FirstOrDefault (e => e.ID == ClubID);
            return View (thisClub);
        }

        [HttpGet ("delete/{ClubID}")]
        public IActionResult DeleteClub (int ClubID) {
            Club thisClub = dbContext.Clubs.FirstOrDefault (e => e.ID == ClubID);
            dbContext.Clubs.Remove (thisClub);
            dbContext.SaveChanges ();
            return Redirect ("/home");

        }

        [HttpGet ("Rsvp/{clubID}")]
        public IActionResult JoinClub(int clubID) {

            int? seshUser = HttpContext.Session.GetInt32 ("ID");
            Club thisClub = dbContext.Clubs.FirstOrDefault (e => e.ID == clubID);
            User thisUser = dbContext.users.FirstOrDefault (u => u.UserId == (int) seshUser);
            Participent newPart = new Participent ();

            List<Participent> yourClubs = dbContext.Participants
                .Where (p => p.UserID == (int) seshUser)
                .Include (p => p.Club)

                .ToList ();

            foreach (var part in yourClubs) {
                if (part.Club.DateOfClub == thisClub.DateOfClub) {
                    TempData["Conflict"] = "You're already booked for that day!";
                    return Redirect ($"/club/{clubID}");
                }
            }

            newPart.Club = thisClub;
            newPart.User = thisUser;
            newPart.UserID = (int) seshUser;
            newPart.ClubID = clubID;
            dbContext.Participants.Add (newPart);
            dbContext.SaveChanges ();
            return Redirect ("/home");
        }

        [HttpGet ("UnRsvp/{clubID}")]
        public IActionResult LeaveClub (int clubID) {

            Club thisClub= dbContext.Clubs.FirstOrDefault (e => e.ID == clubID);
            Participent thisPart;
            int? seshUser = HttpContext.Session.GetInt32 ("ID");
            List<Participent> thisParts = dbContext.Participants
                .Where (p => p.ClubID == clubID).ToList ();
            foreach (var part in thisParts) {
                if (part.UserID == (int) seshUser) {
                    thisPart = part;
                    dbContext.Participants.Remove (thisPart);
                    dbContext.SaveChanges ();
                }
            }
            return Redirect ("/home");
        }

        
    }
}