using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using University.Data.Contexts;

var stringConn=new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("Default");

DbContextOptionsBuilder<AuthDbContext> ops = new DbContextOptionsBuilder<AuthDbContext>();
ops.UseSqlServer(stringConn);
using AuthDbContext context = new AuthDbContext(ops.Options);

context.Students.Add(new() 
{
    Id = "723A1B3",
    Email = "test@gmail.com",
    Name = "Test",
    BirthDate = new DateTime(2000, 1, 1)
});

var res = context.Students.Where(u => u.Email == "test@gmail.com").SingleOrDefault();


Console.WriteLine($"{res.Id} | {res.Name} | {res.Email} | {res.BirthDate:yyyy-MM-dd}");




 
