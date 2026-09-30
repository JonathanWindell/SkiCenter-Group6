using EntityLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Interfaces
{
    public interface IStaffRepository
    {
        Staff GetStaffByEmailAndPassword(string email, string password);
    }
}
