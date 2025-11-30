namespace University.Data.Models;

public class Course
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; }

    public int Credit { get; set; }

    public virtual ICollection<StudentCourse> StudentCourses { get; set; }
}