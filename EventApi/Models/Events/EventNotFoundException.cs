namespace EventApi.Models.Events
{
    public class EventNotFoundException : Exception
    {
        public EventNotFoundException() : base("Событие не найдено.") { }
        public EventNotFoundException(string message) : base(message) { }
        public EventNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
