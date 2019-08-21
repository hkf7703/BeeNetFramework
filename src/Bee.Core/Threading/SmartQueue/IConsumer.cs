

namespace Bee.Threading
{
    /// <summary>
    /// Interface for data consumer
    /// </summary>
    /// <typeparam name="T">Type of the consuming elements</typeparam>
    public interface IConsumer<in T>
    {
        /// <summary>
        /// Pushes new element to the consumer
        /// </summary>
        /// <param name="item">Element</param>
        void Add(T item);
    }
}
