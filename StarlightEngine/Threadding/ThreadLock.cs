using System;
using System.Threading;
using System.Collections.Generic;

namespace StarlightEngine.Threadding
{
    /// <summary>
    /// Provides a simple wrapper arround the C# mutex to force lock hierarchies,
    /// greatly reducing the risk of deadlock.
    /// A thread may only enter locks of lower levels then it currently holds, so that
    /// two threads will never attempt to enter the same locks in different orders - leading to deadlock
    /// </summary>
    public class ThreadLock
    {
        Mutex m_lock = new Mutex();
        int m_level;

        // Per-thread bool to tell if the mutex is locked for that thread
        ThreadLocal<bool> m_locked = new ThreadLocal<bool>();

        // A per-thread static reference to the thread's current level, stored in a stack so it can be reverted
        static ThreadLocal<Stack<int>> m_threadLevel = new ThreadLocal<Stack<int>>();

        public ThreadLock(int level)
        {
            m_level = level;
        }

        /// <summary>
        /// Enters the lock safely, throwing an exception if the thread tries to
        /// enter a lock at or above it's current level
        /// </summary>
        public void EnterLock()
        {
            // Assert m_level < m_lockLevel
            if (m_level >= GetThreadLevel())
            {
                throw new ApplicationException(
                    String.Format("Thread {0} (\"{3}\") attempted to enter a lock level ({1}) at or above it's current level ({2})",
                    Thread.CurrentThread.ManagedThreadId, m_level, GetThreadLevel(), Thread.CurrentThread.Name));
            }

            // Push new level to stack
            GetThreadLevelStack().Push(m_level);

            // Lock mutex
            m_lock.WaitOne();
            m_locked.Value = true;
        }

        /// <summary>
        /// Exits the current lock, restoring the previous thread level
        /// </summary>
        public void ExitLock()
        {
            // Assert that the thread level is equal to the lock level (make sure we're unlocking in the right order)
            if (GetThreadLevel() != m_level){
                throw new ApplicationException("Cannot exit lock which does not match the current thread's level");
            }

            // Unlock mutex
            if (m_locked.Value){
                m_lock.ReleaseMutex();
                m_locked.Value = false;
            }

            // Pop stack
            GetThreadLevelStack().Pop();
        }

        /// <summary>
        /// Allows the user to enter multiple locks of the same level at once,
        /// while still ensuring a constant order
        /// </summary>
        public static void EnterMultiple(params ThreadLock[] locks)
        {
            // Assert that all locks are the same level, check for any locks not at the same level as the first
            bool sameLevel = !Array.Exists(locks, l => l.m_level != locks[0].m_level);
            if (!sameLevel){
                throw new ApplicationException("Thread attempted to enter multiple locks at the same time which are not on the same level");
            }

            // Sort locks by their hash code
            Array.Sort(locks, (lock1, lock2) => lock1.GetHashCode() - lock2.GetHashCode());

            // Lock each lock in order
            for (int i = 0; i < locks.Length; i++){
                locks[i].m_lock.WaitOne();
                locks[i].m_locked.Value = true;
            }

            // Set the thread level
            GetThreadLevelStack().Push(locks[0].m_level);
        }

        /// <summary>
        /// Exits multiple locks that were entered together
        /// </summary>
        public static void ExitMultiple(params ThreadLock[] locks)
        {
            // Assert that all locks are the same level, check for any locks not at the same level as the first
            bool sameLevel = !Array.Exists(locks, l => l.m_level != locks[0].m_level);
            if (!sameLevel){
                throw new ApplicationException("Thread attempted to enter multiple locks at the same time which are not on the same level");
            }

            // Sort locks by their hash code
            Array.Sort(locks, (lock1, lock2) => lock1.GetHashCode() - lock2.GetHashCode());

            // unlock each lock in order
            for (int i = 0; i < locks.Length; i++){
                locks[i].m_lock.ReleaseMutex();
                locks[i].m_locked.Value = false;
            }

            // Set the thread level
            GetThreadLevelStack().Pop();
        }

        /// <summary>
        /// Returns true if the current thread holds this lock
        /// </summary>
        public bool IsLockedByThread(){
            return m_locked.Value;
        }

        /// <summary>
        /// Gets the current level of the thread
        /// </summary>
        public static int GetThreadLevel()
        {
            return GetThreadLevelStack().Peek();
        }

        /// <summary>
        /// Wrapper to get the thread local stack, initializes stack if it's null
        /// </summary>
        private static Stack<int> GetThreadLevelStack()
        {
            if (m_threadLevel.Value == null)
            {
                // create stack if null
                m_threadLevel.Value = new Stack<int>();

                // push max of int to bottom of stack
                m_threadLevel.Value.Push(int.MaxValue);
            }

            return m_threadLevel.Value;
        }
    }
}