using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Entity.Interfaces;
using Sage.SalesLogix.SData.Client;
using Sage.SalesLogix.SData.Client.Test.Diagnostics;
using Sage.SalesLogix.Client;
using Sage.SalesLogix.Client.SData;
using Sage.SalesLogix.Client.SData.Linq;
using Sage.SalesLogix.Client.Hibernate;
using Sage.SalesLogix.Client.Hibernate.Context;

namespace Sage.SalesLogix.SData.Client.Test
{
    class Program
    {
        static void Main(string[] args)
        {


            using (new CustomStopWatch("SData access"))
                PerformClientOperations(new SDataClientContextFactory(), new SDataContextConfiguration() { Servername = "localhost", Port = 3333, Username = "admin", Password = String.Empty });

            using (new HibernateContext("localhost", "SALESLOGIX_EVAL", "admin", String.Empty, @"C:\inetpub\wwwroot\SlxClient"))
            using (new CustomStopWatch("Hibernate access"))
                PerformClientOperations(new HibernateClientContextFactory(), new HibernateContextConfiguration());


            Console.WriteLine("Operations done");


            Console.ReadKey();



            //using (ClientContext context = new ClientContext("localhost", 3333, "admin", String.Empty))
            //{

            //    LoadAccountsAndContacts(context);


            //    //UpdateSample(context);


            //    //CreateNewContactExample(context);
            //}

            //Console.WriteLine("Completed");
            //Console.ReadKey();

        }

        private static void PerformClientOperations(IClientContextFactory contextFactory, IContextConfiguration contextConfiguration)
        {
            using (IClientContext context = ClientFactory.GetContext(contextFactory, contextConfiguration))
            {

                IAccount accountLoaded = context.GetById<IAccount>(context.CreateQuery<IAccount>().SetMaxRows(1).First().Id);
                Console.WriteLine("AccountName: {0}", accountLoaded.AccountName);

                foreach (var item in context.CreateQuery<IAccount>().Include(x => x.Contacts).OrderBy(x => x.AccountName).Where(x => x.AccountName.StartsWith("a")).SetMaxRows(1))
                {
                    Console.WriteLine("Id:{0} Name: {1}", item.Id, item.AccountName);

                    foreach (var contact in context.CreateQuery<IContact>().Where(x => x.Account.Id == item.Id).OrderBy(x => x.Id))
                    {
                            Console.WriteLine("Contact id: {0}; name: {1}", contact.Id, contact.Name);
                    }

                    foreach (var contact in item.Contacts)
                    {
                        Console.WriteLine("Contact id: {0}; name: {1}", contact.Id, contact.Name);
                    }
                }

                //var test = context.CreateQuery<IAccount>().Where(x => x.AccountName.StartsWith("a")).Last();

                //Console.WriteLine(test.AccountName);

                //foreach (var item in context.CreateQuery<IAccount>().OrderBy(x => x.AccountName).SetMaxRows(5).Where(x => x.AccountName.StartsWith("B")))
                //{                                        
                //    Console.WriteLine(context.CreateQuery<IAccount>().Where(x => x.Id == item.Id).Last().AccountName);
                //    Console.WriteLine("{0} {1}", item.AccountName, item.Id);
                //}

                //foreach (var item in context.CreateQuery<IAccount>().SetMaxRows(10).Where(x => x.AccountName.StartsWith("A")).OrderByDescending(x => x.AccountName).Select(x => new { Name = x.AccountName.ToLowerInvariant() }))
                //    Console.WriteLine(item.Name);
            }
        }


        //private static void LoadAccountsAndContacts(ClientContext context)
        //{

        //    List<string> accountIds = new List<string>();

        //    accountIds.AddRange(context.CreateQuery<IAccount>().OrderBy(x => x.AccountName).Select(x => x.Id.ToString()));

        //    var accountQuery = context.CreateQuery<IAccount>().Where(x => accountIds.In(x.Id)).OrderBy(x => x.AccountName);

        //var accountQuery = context.CreateQuery<IAccount>()
        //    .Where(x => x.AlternatePhone == "")
        //    //.Include(x => x.Contacts)
        //    //.Include(x => x.Addresses)
        //    .OrderByDescending(x => x.AccountName);


        //;
        //IEnumerable<IAccount> accountQuery = context.LoadWhere<IAccount>("AccountName like 'a%'", "Contacts", "AccountName");


        //var accountQueryLinq = (from account in context.CreateQuery<IAccount>().SetMaxRows(50)
        //                        orderby account.AccountName ascending,
        //                                account.Industry descending,
        //                                account.Fax,
        //                                account.Email descending
        //                        select account).set;

        //int counter = 0;

        //foreach (var account in accountQuery)
        //{
        //    counter++;
        //    Console.WriteLine(account.AccountName);

        //    Console.WriteLine("{0}, {1}", account.AccountName, ((ClientEntityBase)account).Id.ToString());
        //    Console.WriteLine("====================");

        //    //foreach (var contact in account.Contacts)
        //    //    Console.WriteLine(String.Format("\t{0}", contact.FullName));

        //    //foreach (var address in account.Addresses)
        //    //    Console.WriteLine(String.Format("\t{0}", address.Address1));
        //}

        //Console.WriteLine("{0} accounts loaded", counter);
        //}

        //private static void UpdateSample(ClientContext context)
        //{
        //var accountToEdit = context.LoadById<IAccount>("AA2EK0013096");
        //accountToEdit.AccountName += "x";
        //accountToEdit.Revenue += 1;

        //accountToEdit.Save();
        //Console.WriteLine("Successfully updated Account");
        //}

        //private static void CreateNewContactExample(ClientContext context)
        //{
        //var accountForContact = context.LoadById<IAccount>("AA2EK0013096");

        //var contact = context.CreateNew<IContact>();

        //contact.FirstName = "Robert";
        //contact.LastName = "Reed";
        //contact.Email = "robreed@mail.com";
        //contact.AccountName = accountForContact.AccountName;
        //contact.Account = accountForContact;
        //contact.Address.Description = "Mailing";
        //contact.Address.Address1 = "Test 123";
        //((ClientEntityBase)((object)accountForContact)).RemoveValue("Revenue");

        //var contactSaved = context.CreateNew<IContact>(contact);

        //Console.WriteLine("Successfully created new Contact with ID: {0}", contactSaved.Id.ToString());
        //}
    }
}
