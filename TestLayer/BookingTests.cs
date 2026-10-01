using NUnit.Framework;
using EntityLayer;
using DataLayer;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq;

namespace TestLayer
{
    [TestFixture]
    public class BookingTests
    {
        private SkiCenterDbContext _context;

        [SetUp]
        public void Setup()
        {
            // Initialize database. 
            _context = new SkiCenterDbContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure the DbContext is disposed after each test to avoid resource leaks
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }
       
    }
}
