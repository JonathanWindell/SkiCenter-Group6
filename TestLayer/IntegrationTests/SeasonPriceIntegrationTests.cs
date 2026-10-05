using BusinessLayer.Controllers;
using DataLayer;

namespace TestLayer.IntegrationTests
{
    public class SeasonPriceIntegrationTests
    {
        [TestFixture]
        [Category("Integration")]
        public class CustomerRegistryIntegrationTests
        {
            private SkiCenterDbContext _context;
            private UnitOfWork _unitOfWork;
            private CustomerController _customerController;

            [SetUp]
            public void Setup()
            {
                _context = new SkiCenterDbContext();
                _unitOfWork = new UnitOfWork(_context);
                _customerController = new CustomerController(_unitOfWork);
            }

            [TearDown]
            public void TearDown()
            {
                _unitOfWork?.Dispose();
                _context?.Dispose();
            }
        }
    }
}
