using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Entity.Interfaces;
using Sage.SalesLogix.SData.Client;
using Sage.SalesLogix.SData.Client.Test.Diagnostics;

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


                UpdateSample(context);


                CreateNewContactExample(context);
            }

            Console.ReadKey();

        }

        private static void LoadAccountsAndContacts(ClientContext context)
        {
            foreach (var account in context.LoadWhere<IAccount>("AccountName like 'a%'", "Contacts", "AccountName"))
            {
                Console.WriteLine("{0}, {1}", account.AccountName, ((ClientEntityBase)account).Id.ToString());
                Console.WriteLine("====================");

                foreach (var contact in account.Contacts)
                    Console.WriteLine(String.Format("\t{0}", contact.FullName));
            }
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
