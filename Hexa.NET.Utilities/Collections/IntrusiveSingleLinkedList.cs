namespace HexaModManager.Core.FileDB
{
    using Hexa.NET.Utilities;
    using System;
    using System.Collections;

    public unsafe struct IntrusiveSingleLinkedList<T> : IEnumerable<Pointer<T>> where T : unmanaged, IIntrusiveSingleLinkedListNode<T>
    {
        private T* head;
        private T* tail;
        private nuint count;

        public readonly nuint Count => count;

        public readonly T* Head => head;

        public readonly T* Tail => tail;

        public readonly bool IsEmpty => head == null;

        public T* this[nuint index]
        {
            readonly get
            {
                var current = head;
                nuint i = 0;
                while (current != null)
                {
                    if (i == index)
                    {
                        return current;
                    }
                    current = current->Next;
                    ++i;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                var current = head;
                nuint i = 0;
                T* previous = null;
                while (current != null)
                {
                    if (i == index)
                    {
                        if (previous != null)
                        {
                            previous->Next = value;
                        }
                        else
                        {
                            head = value;
                        }
                        value->Next = current->Next;
                        if (current == tail)
                        {
                            tail = value;
                        }
                        return;
                    }
                    previous = current;
                    current = current->Next;
                    ++i;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public void Append(T* node)
        {
            node->Next = null;
            if (head == null)
            {
                head = node;
                tail = node;
            }
            else
            {
                tail->Next = node;
                tail = node;
            }
            ++count;
        }

        public void Prepend(T* node)
        {
            node->Next = head;
            head = node;
            if (tail == null)
            {
                tail = node;
            }
            ++count;
        }

        public T* PopFront()
        {
            if (head == null)
            {
                return null;
            }
            var node = head;
            head = head->Next;
            if (head == null)
            {
                tail = null;
            }
            --count;
            return node;
        }

        public T* PopBack()
        {
            if (head == null)
            {
                return null;
            }
            if (head == tail)
            {
                var node = head;
                head = null;
                tail = null;
                --count;
                return node;
            }
            var current = head;
            while (current->Next != tail)
            {
                current = current->Next;
            }
            var tailNode = tail;
            current->Next = null;
            tail = current;
            --count;
            return tailNode;
        }

        public void Remove(T* node)
        {
            if (node == head)
            {
                head = node->Next;
                if (head == null)
                {
                    tail = null;
                }
                --count;
                return;
            }

            var current = head;
            while (current != null)
            {
                if (current->Next == node)
                {
                    current->Next = node->Next;
                    if (node == tail)
                    {
                        tail = current;
                    }
                    --count;
                    return;
                }
                current = current->Next;
            }
        }

        public void Clear()
        {
            head = null;
            tail = null;
            count = 0;
        }

        public struct Enumerator : IEnumerator<Pointer<T>>
        {
            private readonly T* head;
            private T* current;

            public Enumerator(T* head)
            {
                this.head = head;
            }

            public readonly Pointer<T> Current => current;

            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (current == null)
                {
                    current = head;
                }
                else
                {
                    current = current->Next;
                }
                return current != null;
            }

            public void Reset()
            {
                current = null;
            }

            public readonly void Dispose()
            {
            }
        }

        public readonly Enumerator GetEnumerator() => new(head);

        readonly IEnumerator<Pointer<T>> IEnumerable<Pointer<T>>.GetEnumerator() => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}