using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.SData.Client.Extensions;
using Sage.SData.Client.Atom;
using Sage.SData.Client.Core;
using Sage.Platform.Orm.Attributes;
using System.Collections;
using Sage.SalesLogix.Client.SData.Linq;
using System.Reflection;

namespace Sage.SalesLogix.Client.SData
{
    /// <summary>
    /// Public context to access sdata in a strongly typed manner. The context offers both, linq functionality and retrieving items using direct methods.
    /// </summary>
    public class SDataClientContext : IClientContext
    {
        private SDataContextConfiguration _configuration;

        private SDataService _Service;

        internal SDataService Service
        {
            get
            {
                if (_Service == null)
                {
                    _Service = new SDataService(String.Format("http://{0}:{1}/sdata/slx/dynamic/-", _configuration.Servername, _configuration.Port), _configuration.Username, _configuration.Password);
                    _Service.Initialize();
                }

                return _Service;
            }
        }

        private static Dictionary<Type, Type> _ClientProxyClasses;

        private static Dictionary<Type, Type> ClientProxyClasses
        {
            get
            {
                if (_ClientProxyClasses == null)
                    _ClientProxyClasses = new Dictionary<Type, Type>();

                return _ClientProxyClasses;
            }
        }

        internal SDataClientContext(SDataContextConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public ICollection<T> GetProxyClients<T>(SDataPayloadCollection items)
        {
            List<T> result = new List<T>();

            foreach (SDataPayload payload in items)
                result.Add(GetProxyClient<T>(payload));

            return result;
        }

        public T GetProxyClients<T>(AtomEntry entry)
        {
            return GetProxyClient<T>(entry.GetSDataPayload());
        }

        public ICollection<T> GetProxyClients<T>(AtomFeed feed)
        {
            List<T> result = new List<T>();

            foreach (AtomEntry entry in feed.Entries)
                result.Add(GetProxyClient<T>(entry.GetSDataPayload()));

            return result;
        }

        public T GetProxyClient<T>(AtomEntry entry)
        {
            return GetProxyClient<T>(entry.GetSDataPayload());
        }

        public T GetProxyClient<T>(SDataPayload entry)
        {
            Type proxyClass = null;

            if (!ClientProxyClasses.ContainsKey(typeof(T)))
            {
                proxyClass = WrapperFactory.GenerateProxyClass(typeof(T));
                ClientProxyClasses.Add(typeof(T), proxyClass);
            }
            else
                proxyClass = ClientProxyClasses[typeof(T)];

            return (T)Activator.CreateInstance(proxyClass, entry, this);
        }

        #region IDisposable Members

        public void Dispose()
        {
            //Noting to do yet
        }

        #endregion

        internal SDataSingleResourceRequest GetRequestForCRUD(SDataClientEntityBase entity)
        {

            Type interfaceClass = GetEntityInterface(entity.GetType());

            SDataSingleResourceRequest request = new SDataSingleResourceRequest(Service);

            request.ResourceKind = GetResourceKind(interfaceClass);

            request.Entry = LoadByIdInternal(interfaceClass, entity.Id.ToString(), null);
            request.Entry.SetSDataPayload(entity._Payload);
            request.ResourceSelector = String.Format("('{0}')", entity.Id);

            return request;

        }

        private Type GetEntityInterface(Type implementedType)
        {
            //Getting Interface
            foreach (var interfaceType in implementedType.GetInterfaces())
                if (interfaceType.FullName.StartsWith("Sage.Entity.Interfaces."))
                    return interfaceType;

            throw new InvalidOperationException(String.Format("Type '{0}' does not implement a SalesLogix interface", implementedType.FullName));
        }

        private static int bulkSize = 100;

        private string GetResourceKind(Type type)
        {
            object[] attributes = type.GetCustomAttributes(typeof(ActiveRecordAttribute), false);

            if (attributes.Length > 0)
                return GetPlural(((ActiveRecordAttribute)attributes[0]).Table);

            throw new InvalidOperationException(String.Format("Type {0} does not contain a valid SalesLogix Table-Attribute", type.FullName));
        }

        /// <summary>
        /// This is stupid! The plural from used for sdata should be in an attribute of the interface
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetPlural(string name)
        {
            name = name.ToLowerInvariant().Trim();

            if (name.EndsWith("y"))
                return name.Substring(0, name.Length - 1) + "ies";

            if (name.EndsWith("s"))
                return name;

            return name + "s";
        }

        #region Public methods to retrieve Entities

        private AtomEntry LoadByIdInternal(Type type, string id, string include)
        {
            SDataSingleResourceRequest request = new SDataSingleResourceRequest(Service);

            request.ResourceKind = GetResourceKind(type);
            request.ResourceSelector = String.Format("('{0}')", id);

            if (!String.IsNullOrEmpty(include))
                request.Include = include;

            return request.Read();
        }

        internal IEnumerable<T> Load<T>(SDataResourceCollectionRequest request)
        {
            int rowsToRead = request.Count;

            request.ResourceKind = GetResourceKind(typeof(T));

            //One request will only return 100 rows, no matter what you do. So this logic repeats the request, until all items are returned.                    

            request.StartIndex = 1;

            bool moreRows = true;

            List<object> result = new List<object>();

            while (moreRows)
            {
                //Set the max-rowcount the the limit of 100 to make sure this code works, even if the limit is removed            
                if (rowsToRead > -1)
                    request.Count = Math.Min(rowsToRead, bulkSize);
                else
                    request.Count = bulkSize;
                
                ICollection<T> tempList = GetProxyClients<T>(request.Read());

                rowsToRead -= tempList.Count;
                moreRows = tempList.Count == bulkSize && rowsToRead != 0;
                request.StartIndex += bulkSize;

                foreach (var item in tempList)
                    yield return item;             
            }

        }        

        #region Obsolete non-Linq queries

        //public ICollection<T> LoadWhere<T>(string filter, string include)
        //{
        //    return LoadWhere<T>(filter, include, null);
        //}

        //public ICollection<T> LoadWhere<T>(string filter, string include, string orderBy)
        //{
        //    SDataResourceCollectionRequest request = new SDataResourceCollectionRequest(Service);

        //    if (!String.IsNullOrEmpty(filter))
        //        request.QueryValues.Add("where", filter);

        //    if (!String.IsNullOrEmpty(include))
        //        request.Include = include;

        //    if (!String.IsNullOrEmpty(orderBy))
        //        request.QueryValues.Add("orderby", orderBy);

        //    return Load<T>(request);
        //}


        //public ICollection<T> LoadWhere<T>(string filter)
        //{
        //    return LoadWhere<T>(filter, null);
        //}

        //public IEnumerable<T> LoadAll<T>(string include, string orderBy)
        //{
        //    return LoadWhere<T>(null, include, orderBy);
        //}

        //public IEnumerable<T> LoadAll<T>(string include)
        //{
        //    return LoadWhere<T>(null, include);
        //}

        //public IEnumerable<T> LoadAll<T>()
        //{
        //    return LoadWhere<T>(null);
        //}

        #endregion

        public T CreateNew<T>()
        {
            SDataTemplateResourceRequest request = new SDataTemplateResourceRequest(Service);

            request.ResourceKind = GetResourceKind(typeof(T));

            return this.GetProxyClient<T>(request.Read());
        }

        public T CreateNew<T>(T item)
        {
            SDataSingleResourceRequest request = new SDataSingleResourceRequest(Service);
            request.ResourceKind = GetResourceKind(typeof(T));

            {
                SDataTemplateResourceRequest requestTemplate = new SDataTemplateResourceRequest(Service);

                requestTemplate.ResourceKind = GetResourceKind(typeof(T));

                request.Entry = requestTemplate.Read();
            }

            request.Entry.SetSDataPayload((((SDataClientEntityBase)((object)item)))._Payload);

            return GetProxyClient<T>(request.Create());
        }

        #endregion

        /// <summary>
        /// Creates a linq Query that can be used to consume SData information
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IQueryable<T> CreateQuery<T>()
        {
            return new SDataQuery<T>(this, new SDataQueryProvider(this, typeof(T)));
        }


        #region IClientContext Members


        public T GetById<T>(object id)
        {
            SDataSingleResourceRequest request = new SDataSingleResourceRequest(Service);

            request.ResourceKind = GetResourceKind(typeof(T));
            request.ResourceSelector = String.Format("('{0}')", id.ToString());

            return GetProxyClients<T>(request.Read());

        }

        #endregion
    }
}
