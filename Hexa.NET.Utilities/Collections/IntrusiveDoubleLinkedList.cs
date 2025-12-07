namespace HexaModManager.Core.FileDB
{
    using Hexa.NET.Utilities;
    using System.Collections;

    public unsafe struct IntrusiveDoubleLinkedList<T> : IEnumerable<Pointer<T>> where T : unmanaged, IIntrusiveDoubleLinkedListNode<T>
    {
        private T* head;
        private T* tail;
        private nuint count;

        public readonly T* Head => head;

        public readonly T* Tail => tail;

        public readonly nuint Count => count;

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
                while (current != null)
                {
                    if (i == index)
                    {
                        value->Prev = current->Prev;
                        value->Next = current->Next;
                        if (current->Prev != null)
                        {
                            current->Prev->Next = value;
                        }
                        else
                        {
                            head = value;
                        }
                        if (current->Next != null)
                        {
                            current->Next->Prev = value;
                        }
                        else
                        {
                            tail = value;
                        }
                        return;
                    }
                    current = current->Next;
                    ++i;
                }
                throw new IndexOutOfRangeException();
            }
        }

        public void Append(T* node)
        {
            node->Next = null;
            node->Prev = tail;
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
            node->Prev = null;
            node->Next = head;
            if (head == null)
            {
                head = node;
                tail = node;
            }
            else
            {
                head->Prev = node;
                head = node;
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
            if (head != null)
            {
                head->Prev = null;
            }
            else
            {
                tail = null;
            }
            --count;
            return node;
        }

        public T* PopBack()
        {
            if (tail == null)
            {
                return null;
            }
            var node = tail;
            tail = tail->Prev;
            if (tail != null)
            {
                tail->Next = null;
            }
            else
            {
                head = null;
            }
            --count;
            return node;
        }

        public void Remove(T* node)
        {
            if (node->Prev != null)
            {
                node->Prev->Next = node->Next;
            }
            else
            {
                head = node->Next;
            }
            if (node->Next != null)
            {
                node->Next->Prev = node->Prev;
            }
            else
            {
                tail = node->Prev;
            }
            --count;
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
                current = null;
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

            public bool MovePrevious()
            {
                if (current == null)
                {
                    return false;
                }
                else
                {
                    current = current->Prev;
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