using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class SkiShopController
    {
        private readonly IUnitOfWork _unitOfWork;

        public SkiShopController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


    }
}