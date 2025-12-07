namespace HexaModManager.Core.FileDB
{
    public unsafe interface IIntrusiveSingleLinkedListNode<T> where T : unmanaged, IIntrusiveSingleLinkedListNode<T>
    {
        public T* Next { get; set; }
    }
}