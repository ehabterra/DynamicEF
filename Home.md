**Dynamic Entity Framework**
Dynamic Entity Framework to allow changing the context and database tables at run-time.

This project is an example about how to "create code first entity framework" that supports adding/editing automatically types at run-time and update database.

_[Documentation](Documentation)_

**+Used Tools/Libraries:+**
* Visual Studio 2015
* Entity Framework 6.1.3
* CodeDom Compiler (for creating assemblies from classes)
* .Net Reflection
* SQL Server 2008 or above

**+Content:+**
* Library Project for dynamic Entity Framework.
* Test Project to illustrate the usage of the library.

**+Notes:+**
* consider to change Connection String in app.config in Test Project before testing the functionalities.
 {{  <connectionStrings>
    <add name="DBConnection" connectionString="Data Source=(localdb)\v11.0;Initial Catalog=TestDB;Trusted_Connection=yes;Integrated Security=SSPI;" providerName="System.Data.SqlClient" />
  </connectionStrings>
}}
* don't hesitate to contact me if you have any question. 

**+Last but not least:+**
I want to thank community for the numerous articles and codes which helped me in learning curve especially my favorite website www.stackoverflow.com.
