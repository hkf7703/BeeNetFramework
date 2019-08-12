

namespace Bee.Threading
{
    /// <summary>
    /// Interface for data producer
    /// </summary>
    /// <typeparam name="T">Type of the produced elements</typeparam>
    public interface IProducer<T>
    {
        /// <summary>
        /// Takes new item from the producer
        /// </summary>
        /// <returns>Taken item</returns>
        T Take();
    }
}
