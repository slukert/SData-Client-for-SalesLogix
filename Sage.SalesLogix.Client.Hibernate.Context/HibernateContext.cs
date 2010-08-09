using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Platform.Application;
using Sage.Platform.Data;
using Sage.Platform.Security;
using Sage.SalesLogix.Security;
using System.IO;
using Sage.Platform.Configuration;
using Sage.Platform.DynamicMethod;
using System.Data.OleDb;

namespace Sage.SalesLogix.Client.Hibernate.Context
{
    public class HibernateContext : IDisposable
    {

        public class ClientUserService : SLXUserService
        {
            internal static string username;

            public override string UserId
            {
                get
                {
                    return username.ToUpper();
                }
            }

            public override string UserName
            {
                get
                {
                    return username;
                }
            }
        }

        public class ClientDataService : IDataService
        {

            internal static string database;
            internal static string server;
            internal static string username;
            internal static string password;

            #region IDataService Members

            public string Alias
            {
                get { return database; }
            }

            public string Database
            {
                get { return database; }
            }

            public System.Data.IDbConnection GetConnection()
            {
                return new OleDbConnection(GetConnectionString());
            }

            public string GetConnectionString()
            {
                return String.Format("Provider=SLXOLEDB.1;Data Source={0};Initial Catalog={1};User ID={2};Password={3}",
                    Server, Database, username, password);
            }

            public System.Data.Common.DbProviderFactory GetDbProviderFactory()
            {
                throw new NotImplementedException();
            }

            public string Server
            {
                get { return server; }
            }

            public string VenderVersion
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

        WorkItem _workItem;
        
        public HibernateContext(string server, string database, string username, string password, string configurationFileLocation)
        {
            ClientDataService.database = database;
            ClientDataService.server = server;
            ClientDataService.username = username;
            ClientDataService.password = password;
            ClientUserService.username = username;

            string applicationName = "Sage.SalesLogix.ClientContext";

            _workItem = ApplicationContext.Initialize(applicationName);

            CopyConfigurationFiles(configurationFileLocation, applicationName);

            _workItem.Services.AddNew(typeof(ClientDataService), typeof(IDataService));
            _workItem.Services.AddNew(typeof(ClientUserService), typeof(IUserService));

        }

        private void CopyConfigurationFiles(string configurationFileLocation, string applicationName)
        {
            DirectoryInfo targetDir = new DirectoryInfo(@"Configuration\Application\" + applicationName);

            if (!targetDir.Exists)
                targetDir.Create();
            
            ConfigurationManager configManager = _workItem.Services.Get<ConfigurationManager>();

            //Copy and register hibernate config
            {
                File.Copy(Path.Combine(configurationFileLocation, "hibernate.xml"), Path.Combine(targetDir.FullName, "hibernate.xml"), true);

                ReflectionConfigurationTypeInfo typeInfo = new ReflectionConfigurationTypeInfo(typeof(HibernateConfiguration));
                typeInfo.ConfigurationSourceType = typeof(FileConfigurationSource);
                configManager.RegisterConfigurationType(typeInfo);
            }

            //Copy and register dynamicmethods
            {
                File.Copy(Path.Combine(configurationFileLocation, "dynamicmethods.xml"), Path.Combine(targetDir.FullName, "dynamicmethods.xml"), true);

                ReflectionConfigurationTypeInfo typeInfo = new ReflectionConfigurationTypeInfo(typeof(DynamicMethodConfiguration));
                typeInfo.ConfigurationSourceType = typeof(FileConfigurationSource);
                configManager.RegisterConfigurationType(typeInfo);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {

            if (_workItem != null)
                _workItem.Dispose();

            ApplicationContext.Shutdown();
        }

        #endregion
    }
}
