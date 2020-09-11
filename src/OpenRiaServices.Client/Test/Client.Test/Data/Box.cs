extern alias SSmDsClient;

namespace OpenRiaServices.DomainServices.Client.Test
{
    /// <summary>
    /// Helper class used to force a value type to be allocated on the heap
    /// It is useful if values in async statemachines needs to be passed to callbacks
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Box<T> where T : struct
    {
        private T _value;

        public Box(T value) => _value = value;

        public ref T Value => ref _value;
    }
}
