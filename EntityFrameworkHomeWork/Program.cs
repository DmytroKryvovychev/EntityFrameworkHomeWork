using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace EntityFrameworkHomeWork
{
    public class Faculty
    {
        public int Id { get; set; }
        
        public int Number { get; set; }
        [MaxLength(250)]
        public string Title { get; set; }
        [Required]
        public ICollection<Group> Groups { get; set; }

    }

    public class Group
    {
        public int Id { get; set; }

        [MaxLength(20)]
        public string Number { get; set; }

        [MaxLength(250)]
        public string Title { get; set; }

        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; }
        [Required]
        public ICollection<Student> Students { get; set; }

        public Group(string number, string title)
        {
            Number = number;
            Title = title;
        }

        public Group(string number, string title, ICollection<Student> students) : this(number, title)
        {
            Students = students;
        }

    }

    public class Student
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; }


        public Student(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }



    }

    public class DatabaseContext : DbContext
    {
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Student> Students { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=./university.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Faculty>()
                .HasMany(f => f.Groups)
                .WithOne(g => g.Faculty)
                .HasForeignKey(g => g.FacultyId);

            modelBuilder.Entity<Group>()
                .HasMany(f => f.Students)
                .WithOne(g => g.Group)
                .HasForeignKey(g => g.GroupId);
        }
    }

    class Program
    {

        static async Task DoDatabaseWork()
        {
            await using (var db = new DatabaseContext())
            {
                await db.Database.EnsureCreatedAsync();

                await using var transaction = await db.Database.BeginTransactionAsync();
                {
                    try
                    {
                        var faculty1 = new Faculty() 
                        {
                            Title = "Ракетно-космические технологии",
                            Number = 5,
                            Groups = new List<Group>()
                            {
                                new Group("410", "Ионно-плазменные двигателя")
                                {
                                    Students = new List<Student>()
                                    {
                                        new Student("Dmytro", "Kryvovychev"),
                                        new Student("Oleg", "Zoryn"),
                                        new Student("Ekaterina", "Zoryna"),
                                        new Student("Inna", "Snysarenko")
                                    }
                                },
                                new Group("414", "Ракетные двигатели")
                                {
                                    Students = new List<Student>()
                                    {
                                        new Student("Dmytro", "Shapoval"),
                                        new Student("Lev", "Ivanov"),
                                        new Student("Ivan", "Dmytryev"),
                                        new Student("Olga", "Kiparis")

                                    }
                                }
                            }
                        };

                        await db.Faculties.AddAsync(faculty1);
                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                    }

                }
            }

            await using (var db = new DatabaseContext())
            {

                await db.Faculties
                    .Include(f => f.Groups)
                    .ThenInclude(s => s.Students)
                    .ForEachAsync(f => 
                    {
                    Console.WriteLine("Faculty title is " + f.Title + " and its number is " + f.Number);
                    Console.WriteLine("The faculty includes the following groups:");
                    foreach (var group in f.Groups)
                    {
                        Console.WriteLine("Group title is " + group.Title + " and its number(code) is " + group.Number);
                        Console.WriteLine("The group includes the following students:");

                        foreach (var student in group.Students)
                        {
                            Console.WriteLine("Name is " + student.FirstName + " " + student.LastName);
                        }
                    }
                    });
            }


        }
        
        static void Main(string[] args)
        {
            DoDatabaseWork().Wait();
        }
    }
}
