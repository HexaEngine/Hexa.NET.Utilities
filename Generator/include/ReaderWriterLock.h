#ifndef HEXA_UTILS_ReaderWriterLock_H
#define HEXA_UTILS_ReaderWriterLock_H

#include "common.h"
#include <stdio.h>

/**
 * @brief A lock-free reader-writer lock.
 *
 * Allows multiple concurrent readers or a single exclusive writer.
 * Writers are given fairness: once a writer is waiting, new readers
 * will block until the writer has acquired and released the lock.
 *
 * Must be initialized with ReaderWriterLock_Init() before use.
 */
typedef struct ReaderWriterLock_t
{
	size_t storage;
} ReaderWriterLock;

/**
 * @brief Initializes a ReaderWriterLock.
 * @param cLock Pointer to the lock to initialize.
 */
HEXA_UTILS_API(void) ReaderWriterLock_Init(ReaderWriterLock* cLock);

/**
 * @brief Acquires a read lock, blocking until it is available.
 *
 * Blocks if a writer currently holds or is waiting for the lock.
 *
 * @param cLock Pointer to the lock.
 * @return 1 on success, -1 if the reader count would overflow.
 */
HEXA_UTILS_API(int) ReaderWriterLock_LockRead(ReaderWriterLock* cLock);

/**
 * @brief Attempts to acquire a read lock without blocking.
 *
 * @param cLock Pointer to the lock.
 * @return 1 if the read lock was acquired, 0 if a writer holds or is waiting
 *         for the lock, -1 if the reader count would overflow.
 */
HEXA_UTILS_API(int) ReaderWriterLock_TryLockRead(ReaderWriterLock* cLock);

/**
 * @brief Releases a previously acquired read lock.
 * @param cLock Pointer to the lock.
 */
HEXA_UTILS_API(void) ReaderWriterLock_UnlockRead(ReaderWriterLock* cLock);

/**
 * @brief Acquires a write lock, blocking until all readers and other writers
 *        have released the lock.
 * @param cLock Pointer to the lock.
 */
HEXA_UTILS_API(void) ReaderWriterLock_LockWrite(ReaderWriterLock* cLock);

/**
 * @brief Attempts to acquire a write lock without blocking.
 *
 * @param cLock Pointer to the lock.
 * @param preserveWriterFairness If true, the call blocks until all active
 *        readers drain before returning success, preventing writer starvation.
 *        If false, the call returns 0 immediately when readers are active.
 * @return 1 if the write lock was acquired, 0 if another writer already holds
 *         the lock (or readers are active and @p preserveWriterFairness is false).
 */
HEXA_UTILS_API(int) ReaderWriterLock_TryLockWrite(ReaderWriterLock* cLock, bool preserveWriterFairness);

/**
 * @brief Releases a previously acquired write lock.
 * @param cLock Pointer to the lock.
 */
HEXA_UTILS_API(void) ReaderWriterLock_UnlockWrite(ReaderWriterLock* cLock);

#endif