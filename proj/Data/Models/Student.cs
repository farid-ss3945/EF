namespace University.Data.Models;

public class Student
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; }
    
    public string Email { get; set; }

    public DateTime BirthDate { get; set; }
    
    public virtual ICollection<StudentCourse> StudentCourses { get; set; }
}