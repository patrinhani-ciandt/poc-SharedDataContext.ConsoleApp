using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace POC.SharedDataContext.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var t01 = Task.Factory.StartNew(() => 
            {
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(1000);

                    Console.WriteLine("Task 01 ({0}): {1}", Thread.CurrentThread.ManagedThreadId, processData());
                    
                }
            });

            var t02 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(1000);

                    Console.WriteLine("Task 02 ({0}): {1}", Thread.CurrentThread.ManagedThreadId, processData());

                }
            });

            Console.WriteLine("-----------------------------");
            Console.ReadLine();
        }

        private static string processData()
        {
            const string myDataKey = "KeyProcessData_MyData";

            SharedDataContext.CurrentContext.Activate();

            string mydata = "Default non shared data";

            string myshareddata = null;

            if (SharedDataContext.CurrentContext.IsActivated())
            {
                myshareddata = SharedDataContext.CurrentContext.GetData<string>(myDataKey);

                //Add the values if not exists
                if (myshareddata == null)
                {
                    myshareddata = string.Format("My Data on Thread: {0}", Thread.CurrentThread.ManagedThreadId);

                    SharedDataContext.CurrentContext.SetData(myDataKey, myshareddata);
                }
            }

            return (myshareddata != null) ? myshareddata : mydata;
        }
    }

    public class SharedDataContext
    {
        //public const string KeySharedDataInitialization = "SharedDataContext_Init";
        static LocalDataStoreSlot sharedDataContextDataStoreSlot = Thread.AllocateNamedDataSlot("SharedDataContext_Init");

        private static Dictionary<string, LocalDataStoreSlot> myDataSlots = new Dictionary<string, LocalDataStoreSlot>();

        #region Singleton implementation

        private static volatile SharedDataContext currentContext;
        private static object syncRoot = new Object();

        private SharedDataContext() { }

        public static SharedDataContext CurrentContext
        {
            get
            {
                if (currentContext == null)
                {
                    lock (syncRoot)
                    {
                        if (currentContext == null)
                            currentContext = new SharedDataContext();
                    }
                }

                return currentContext;
            }
        }

        #endregion Singleton implementation

        public void Activate()
        {
            Thread.SetData(sharedDataContextDataStoreSlot, true);
        }

        public bool IsActivated()
        {
            return (Thread.GetData(sharedDataContextDataStoreSlot) as bool?) ?? false;
        }

        public TData GetData<TData>(string dataKey)
        {
            var isInit = (Thread.GetData(sharedDataContextDataStoreSlot) as bool?) ??  false;

            if ((isInit) && (myDataSlots.ContainsKey(dataKey)))
            {
                var slotData = Thread.GetData(myDataSlots[dataKey]);

                var mydata = (slotData != null) ? (TData)slotData : default(TData);

                return mydata;
            }

            return default(TData);
        }

        public void SetData<TData>(string dataKey, TData myData)
        {
            var isInit = (Thread.GetData(sharedDataContextDataStoreSlot) as bool?) ?? false;

            if (!isInit) return;

            LocalDataStoreSlot myDataDataStoreSlot = null;

            if (!myDataSlots.ContainsKey(dataKey))
            {
                myDataDataStoreSlot = Thread.AllocateNamedDataSlot(dataKey);

                myDataSlots[dataKey] = myDataDataStoreSlot;
            }
            else
            {
                myDataDataStoreSlot = myDataSlots[dataKey];
            }

            Thread.SetData(myDataDataStoreSlot, myData);
        }
    }
}
