using System;
using System.Collections.Generic;
using System.Linq;
using DataLayer.Interfaces;
using EntityLayer;

namespace BusinessLayer.Controllers
{
    public class BookingController
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
    }


}
