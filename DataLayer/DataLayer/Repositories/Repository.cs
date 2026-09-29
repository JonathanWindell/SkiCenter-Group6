using System;
using System.Collections.Generic;
using System.Text;
using DataLayer.Interfaces;

namespace DataLayer.Repositories
{
    /// <summary>
    /// Provides a generic implementation of the base repository pattern.
    /// Standardizes fundamental CRUD operations for all database entities.
    /// </summary>
    /// <typeparam name="T">The entity type this repository handles.</typeparam>
    public abstract class Repository<T> : IRepository<T> where T : class
    {
        protected readonly SkiCenterDbContext _context;

        /// <summary>
        /// Initializes the base repository with the shared database context.
        /// </summary>
        /// <param name="context">The data context for database communication.</param>
        protected Repository(SkiCenterDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a new entity to the change tracker to be persisted on the next save.
        /// </summary>
        /// <param name="entity">The object to be inserted.</param>
        public void Add(T entity)
        {
            _context.Set<T>().Add(entity);
        }

        /// <summary>
        /// Marks an existing entity for removal from the database.
        /// </summary>
        /// <param name="entity">The object to be deleted.</param>
        public void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        /// <summary>
        /// Notifies the context that an entity has been modified.
        /// </summary>
        /// <param name="entity">The object with updated values.</param>
        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }

        /// <summary>
        /// Searches for a specific record using its primary key.
        /// </summary>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <returns>The found entity or null if no match exists.</returns>
        public T GetByID(int id)
        {
            return _context.Set<T>().Find(id);
        }
    }
}
