namespace Bee.Threading
{
    /// <summary>
    /// Consolidated interface for data producer and data consumer
    /// </summary>
    /// <typeparam name="T">Type of the produced/consumed elements</typeparam>
    public interface IProducerConsumer<T>: IProducer<T>, IConsumer<T>
    {
    }
}
