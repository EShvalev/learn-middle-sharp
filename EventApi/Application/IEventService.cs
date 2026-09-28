using EventApi.Models;
using System.Runtime.InteropServices;

namespace EventApi.Application
{
    public interface IEventService
    {
        public List<Event> GetAll();
        public Event? GetById(int id);
        public int Create(Event evnt);
        public Event? Update(int id, Event newevnt);
        public void Delete(int id);
    }
}
