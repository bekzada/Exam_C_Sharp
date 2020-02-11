using Microsoft.EntityFrameworkCore;



namespace FridayExam.Models
{
    public class HomeContext : DbContext
    {
       
        public HomeContext(DbContextOptions options) : base(options){} 
        
        public DbSet<User> users {get; set;}

        public DbSet<Club> Clubs {get; set;}

        public DbSet<Participent> Participants { get; set; }
       
    }
}