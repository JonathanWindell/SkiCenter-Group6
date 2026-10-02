namespace DataLayer.Interfaces
{
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Generic method for creating Entity 
        /// <summary>
        void Add(T entity);


        /// <summary>
        /// Generic method for deleting entity
        /// <summary>
        void Delete(T entity);

        /// <summary>
        /// Generic method for updating Entity 
        /// <summary>
        void Update(T entity);

        /// <summary>
        /// Generic method for getting Entity by ID
        /// <summary>
        T GetByID(int id);
    }
}
