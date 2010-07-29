using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Entity.Interfaces;
using Sage.SalesLogix.SData.Client;
using Sage.SalesLogix.SData.Client.Test.Diagnostics;
using Sage.SalesLogix.SData.Client.Linq;

namespace Sage.SalesLogix.SData.Client.Test
{
    class Program
    {
        static void Main(string[] args)
        {

            using (new CustomStopWatch("SData access"))
            using (ClientContext context = new ClientContext("localhost", 3333, "admin", String.Empty))
            {

                LoadAccountsAndContacts(context);


                //UpdateSample(context);


                //CreateNewContactExample(context);
            }

            Console.WriteLine("Completed");
            Console.ReadKey();

        }

        private static void LoadAccountsAndContacts(ClientContext context)
        {

            List<string> accountIds = new List<string>();

            accountIds.AddRange(context.CreateQuery<IAccount>().OrderBy(x => x.AccountName).Select(x => x.Id.ToString()));                     

            var accountQuery = context.CreateQuery<IAccount>().Where(x => accountIds.In(x.Id)).OrderBy(x => x.AccountName);

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

            int counter = 0;

            foreach (var account in accountQuery)
            {
                counter++;
                Console.WriteLine(account.AccountName);

                Console.WriteLine("{0}, {1}", account.AccountName, ((ClientEntityBase)account).Id.ToString());
                Console.WriteLine("====================");

                //foreach (var contact in account.Contacts)
                //    Console.WriteLine(String.Format("\t{0}", contact.FullName));

                //foreach (var address in account.Addresses)
                //    Console.WriteLine(String.Format("\t{0}", address.Address1));
            }

            Console.WriteLine("{0} accounts loaded", counter);
        }

        private static void UpdateSample(ClientContext context)
        {
            var accountToEdit = context.LoadById<IAccount>("AA2EK0013096");
            accountToEdit.AccountName += "x";
            accountToEdit.Revenue += 1;

            accountToEdit.Save();
            Console.WriteLine("Successfully updated Account");
        }

        private static void CreateNewContactExample(ClientContext context)
        {
            var accountForContact = context.LoadById<IAccount>("AA2EK0013096");

            var contact = context.CreateNew<IContact>();

            contact.FirstName = "Robert";
            contact.LastName = "Reed";
            contact.Email = "robreed@mail.com";
            contact.AccountName = accountForContact.AccountName;
            contact.Account = accountForContact;
            contact.Address.Description = "Mailing";
            contact.Address.Address1 = "Test 123";
            ((ClientEntityBase)((object)accountForContact)).RemoveValue("Revenue");

            var contactSaved = context.CreateNew<IContact>(contact);

            Console.WriteLine("Successfully created new Contact with ID: {0}", contactSaved.Id.ToString());
        }
    }
}
