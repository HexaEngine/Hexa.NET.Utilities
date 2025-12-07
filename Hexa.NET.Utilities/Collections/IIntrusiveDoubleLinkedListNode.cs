namespace HexaModManager.Core.FileDB
{
    public unsafe interface IIntrusiveDoubleLinkedListNode<T> where T : unmanaged, IIntrusiveDoubleLinkedListNode<T>
    {
        public T* Next { get; set; }

        public T* Prev { get; set; }
    }
}