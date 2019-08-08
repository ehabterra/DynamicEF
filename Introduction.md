+**Introduction**+
The tool is an initial step towards flexible system that enables super user to edit anything or even more the user can add new functionalities to the system (Involve users ;) ).

I begun with data layer because it's the base layer that eveything depends on, to handle data layer I used Entity Framework to use it's cool stuff (to not reinvent the wheel).

There are many tries to make EF dealing with types (Entities) that are created at run-time for example:
# [https://romiller.com/2012/03/26/dynamically-building-a-model-with-code-first/](https://romiller.com/2012/03/26/dynamically-building-a-model-with-code-first/)
# [http://www.codeproject.com/Articles/839640/Unknown-Class-Dynamically-Generated-at-runtime-for](http://www.codeproject.com/Articles/839640/Unknown-Class-Dynamically-Generated-at-runtime-for)

The difference here is to let the user build his/her class by him/her self that will be soon a table in the database, this could be done by open text file or c# editor and write a class with attributes as s/he want, for example:
{{
using System.ComponentModel.DataAnnotations;

public class table1
{
    public int Id { get; set; }

    [Required()](Required())
    public string Title { get; set; }
}
}}

After that place it in the classes directory and it will generate the dll (assembly) and update the database, so you can use it to insert, update, delete and select from it.

Next you'll see how to do that in few steps with code examples.
[How it works](How-it-works.md)
