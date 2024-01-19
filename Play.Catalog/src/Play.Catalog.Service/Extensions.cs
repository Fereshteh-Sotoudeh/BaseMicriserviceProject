using Play.Catalog.Service.Entities;

namespace Play.Catalog.Service;
public static class Extensions
{
    public static ItemDto AsDto(this Item item)
    {
        return new ItemDto(Id: item.Id, Name: item.Name, Description: item.Description, Price: item.Price, CreatedDate: item.CreatedDate);
    }
}