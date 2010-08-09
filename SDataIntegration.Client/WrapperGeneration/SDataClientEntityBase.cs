using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.SData.Client.Extensions;
using Sage.SData.Client.Core;
using Sage.SData.Client.Atom;
using System.Globalization;

namespace Sage.SalesLogix.Client.SData
{

    /// <summary>
    /// Base class for all dynamically generated wrappers
    /// </summary>
    public abstract class SDataClientEntityBase
    {
        internal protected SDataPayload _Payload;
        protected SDataClientContext _Context;

        public SDataClientEntityBase(SDataPayload payload, SDataClientContext context)
        {
            this._Payload = payload;
            this._Context = context;
        }

        #region IPersistentEntity Members

        protected T GetPrimitiveValue<T>(string key)
        {
            if (!_Payload.Values.ContainsKey(key))
                return default(T);

            object value = _Payload.Values[key];

            Type targetType = typeof(T);

            //Different types used, require casts
            if (targetType == typeof(String))
                return (T)value;

            if (targetType == typeof(bool) || targetType == typeof(Nullable<bool>))
                return (T)((object)Convert.ToBoolean(value, CultureInfo.InvariantCulture));

            if (targetType == typeof(DateTime) || targetType == typeof(Nullable<DateTime>))
                return (T)((object)Convert.ToDateTime(value, CultureInfo.InvariantCulture));

            if (targetType == typeof(Int16) || targetType == typeof(Nullable<Int16>))
                return (T)((object)Convert.ToInt16(value, CultureInfo.InvariantCulture));

            if (targetType == typeof(Int32) || targetType == typeof(Nullable<Int32>))
                return (T)((object)Convert.ToInt32(value, CultureInfo.InvariantCulture));

            if (targetType == typeof(Int64) || targetType == typeof(Nullable<Int64>))
                return (T)((object)Convert.ToInt64(value, CultureInfo.InvariantCulture));

            if (targetType == typeof(double) || targetType == typeof(Nullable<double>))
                return (T)((object)Convert.ToDouble(value, CultureInfo.InvariantCulture));

            if (targetType == typeof(decimal) || targetType == typeof(Nullable<decimal>))
                return (T)((object)Convert.ToDecimal(value, CultureInfo.InvariantCulture));

            return (T)((object)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));          

        }

        protected void SetPrimitiveValue<T>(string key, T value)
        {
            _Payload.Values[key] = ConvertToPrimitiveValue<T>(value);
        }

        private object ConvertToPrimitiveValue<T>(T value)
        {
            Type targetType = typeof(T);

            if (value == null || targetType == typeof(String))
                return value;

            //There seems to be some issues with formats of values
            if (targetType.FullName.StartsWith("Sage.Entity.Interfaces."))
                return ((SDataClientEntityBase)((object)value))._Payload;

            if (targetType == typeof(bool) || targetType == typeof(Nullable<bool>))
                return ((bool)((object)value)).ToString(CultureInfo.InvariantCulture);

            if (targetType == typeof(DateTime) || targetType == typeof(Nullable<DateTime>))
                return ((DateTime)((object)value)).ToString(CultureInfo.InvariantCulture);

            if (targetType == typeof(Int16) || targetType == typeof(Nullable<Int16>))
                return ((Int16)((object)value)).ToString(CultureInfo.InvariantCulture);

            if (targetType == typeof(Int32) || targetType == typeof(Nullable<Int32>))
                return ((Int32)((object)value)).ToString(CultureInfo.InvariantCulture);

            if (targetType == typeof(Int64) || targetType == typeof(Nullable<Int64>))
                return ((Int64)((object)value)).ToString(CultureInfo.InvariantCulture);

            if (targetType == typeof(double) || targetType == typeof(Nullable<double>))
                return ((double)((object)value)).ToString(CultureInfo.InvariantCulture);

            if (targetType == typeof(decimal) || targetType == typeof(Nullable<decimal>))
                return ((decimal)((object)value)).ToString();

            return value;
        }

        public void Delete()
        {
            SDataSingleResourceRequest request = _Context.GetRequestForCRUD(this);

            if (request.Delete())
                throw new InvalidOperationException(String.Format("Error deleting {0} {1}.", request.ResourceKind, request.ResourceSelector));

            OnPersisted(new EventArgs());
        }

        public event EventHandler<EventArgs> Persisted;

        private void OnPersisted(EventArgs e)
        {
            if (Persisted != null)
                Persisted(this, e);
        }

        public Sage.Platform.Orm.Interfaces.PersistentState PersistentState
        {
            get { throw new NotImplementedException(); }
        }

        public void Save()
        {
            SDataSingleResourceRequest request = _Context.GetRequestForCRUD(this);

            AtomEntry result = request.Update();

            if (result.GetSDataHttpStatus() != 200)
                throw new InvalidOperationException(String.Format("Error updating {0} {1}. Errorcode: {2}", request.ResourceKind, request.ResourceSelector, result.GetSDataHttpStatus()));

            _Payload = result.GetSDataPayload();

            OnPersisted(new EventArgs());
        }



        #endregion

        #region IDynamicEntity Members

        public object this[string propertyName]
        {
            get
            {
                return _Payload.Values[propertyName];
            }
            set
            {
                _Payload.Values[propertyName] = value;
            }
        }

        #endregion

        #region IComponentReference Members

        public object Id
        {
            get { return _Payload.Key; }
        }

        #endregion

       
        public void RemoveValue(string value)
        {
            if (_Payload.Values.ContainsKey(value))
                _Payload.Values.Remove(value);
        }
    }
}
