using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;



namespace FridayExam.Models
{
    public class Participent
    {
        [Key]
        public int ID{get;set;}
        public int UserID{get;set;}
        public int ClubID{get;set;}
        public Club Club{ get;set;}
        public User User{get;set;}
        
    }
}