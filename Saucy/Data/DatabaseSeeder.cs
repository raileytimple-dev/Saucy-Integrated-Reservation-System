using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaucyMVC.Models;
namespace SaucyMVC.Data;
public static class DatabaseSeeder {
 public static async Task SeedAsync(SaucyDbContext db){
  if(!await db.Users.AnyAsync()){
   var h=new PasswordHasher<User>();
   var admin=new User{FullName="Saucy Administrator",Email="admin@saucy.local",PhoneNumber="",Role="Admin"}; admin.PasswordHash=h.HashPassword(admin,"Admin123!");
   var staff=new User{FullName="Saucy Staff",Email="staff@saucy.local",PhoneNumber="",Role="Staff"}; staff.PasswordHash=h.HashPassword(staff,"Staff123!");
   var customer=new User{FullName="Demo Customer",Email="customer@saucy.local",PhoneNumber="09123456789",Role="Customer"}; customer.PasswordHash=h.HashPassword(customer,"Customer123!");
   db.Users.AddRange(admin,staff,customer);
  }
  if(!await db.DiningTables.AnyAsync()) db.DiningTables.AddRange(new DiningTable{TableNumber="T01",Capacity=2},new DiningTable{TableNumber="T02",Capacity=2},new DiningTable{TableNumber="T03",Capacity=4},new DiningTable{TableNumber="T04",Capacity=4},new DiningTable{TableNumber="T05",Capacity=6},new DiningTable{TableNumber="T06",Capacity=8});
  if(!await db.SauceOptions.AnyAsync()) db.SauceOptions.AddRange(
   new SauceOption{Name="C",ImagePath="/legacy/Images/C.png",DisplayOrder=1},
   new SauceOption{Name="Garlic Mayo",ImagePath="/legacy/Images/CGM.png",DisplayOrder=2},
   new SauceOption{Name="Cheese n' Cheese",ImagePath="/legacy/Images/CNC.png",DisplayOrder=3},
   new SauceOption{Name="GC",ImagePath="/legacy/Images/GC.png",DisplayOrder=4},
   new SauceOption{Name="HG",ImagePath="/legacy/Images/HG.png",DisplayOrder=5},
   new SauceOption{Name="HM",ImagePath="/legacy/Images/HM.png",DisplayOrder=6},
   new SauceOption{Name="Korean Barbecue",ImagePath="/legacy/Images/KBBQ.png",DisplayOrder=7},
   new SauceOption{Name="M",ImagePath="/legacy/Images/M.png",DisplayOrder=8},
   new SauceOption{Name="Sriracha",ImagePath="/legacy/Images/S.png",DisplayOrder=9},
   new SauceOption{Name="SC",ImagePath="/legacy/Images/SC.png",DisplayOrder=10},
   new SauceOption{Name="TB",ImagePath="/legacy/Images/TB.png",DisplayOrder=11},
   new SauceOption{Name="WP",ImagePath="/legacy/Images/WP.png",DisplayOrder=12});

  // CF is the Chicken Fries product artwork, not a sauce. Remove the old
  // sauce entry from databases created by earlier versions of the seed data.
  var invalidChickenFriesSauces=await db.SauceOptions.Where(x=>x.ImagePath=="/legacy/Images/CF.png").ToListAsync();
  if(invalidChickenFriesSauces.Count>0)db.SauceOptions.RemoveRange(invalidChickenFriesSauces);
  var knownSauceNames=new Dictionary<string,(string Code,string Name)>{
   ["/legacy/Images/CGM.png"]=("CGM","Garlic Mayo"),
   ["/legacy/Images/CNC.png"]=("CNC","Cheese n' Cheese"),
   ["/legacy/Images/KBBQ.png"]=("KBBQ","Korean Barbecue"),
   ["/legacy/Images/S.png"]=("S","Sriracha")
  };
  var knownSaucePaths=knownSauceNames.Keys.ToList();
  var existingSauces=await db.SauceOptions.Where(x=>knownSaucePaths.Contains(x.ImagePath)).ToListAsync();
  foreach(var sauce in existingSauces){var known=knownSauceNames[sauce.ImagePath];if(sauce.Name==known.Code)sauce.Name=known.Name;}
  var menuSeed=new[]{
   new MenuItem{Name="Chicken Fries",Category="Famous Offerings",Price=110,Description="Crispy chicken strips served with fries, rice, and house dip.",ImagePath="/legacy/FamousOfferings/FOCF.png"},
   new MenuItem{Name="Pork Bacon Ribs",Category="Famous Offerings",Price=110,Description="Tender glazed pork ribs served with steamed rice.",ImagePath="/legacy/FamousOfferings/FOPBR.png"},
   new MenuItem{Name="Pot Roast Beef",Category="Famous Offerings",Price=110,Description="Slow-cooked roast beef with savory gravy, vegetables, and rice.",ImagePath="/legacy/FamousOfferings/FOPRB.png"},
   new MenuItem{Name="Saucy Fried Chicken",Category="Famous Offerings",Price=110,Description="House-style fried chicken with Saucy's signature flavor.",ImagePath="/legacy/FamousOfferings/FOSF.png"},
   new MenuItem{Name="Mushroom Alfredo",Category="Pasta",Price=140,Description="Creamy mushroom Alfredo served with toasted garlic bread.",ImagePath="/legacy/Pasta/PMA.png"},
   new MenuItem{Name="Tomato and Herb",Category="Pasta",Price=140,Description="Rotini pasta tossed with tomatoes, herbs, and fresh basil.",ImagePath="/legacy/Pasta/PTH.png"},
   new MenuItem{Name="Truly Spaghetti",Category="Pasta",Price=80,Description="Classic spaghetti in rich tomato sauce with basil and cheese.",ImagePath="/legacy/Pasta/PTS.png"},
   new MenuItem{Name="Cheesy Garlic Bread",Category="Sides & Drinks",Price=40,Description="Golden garlic bread finished with melted cheese and herbs.",ImagePath="/legacy/SideDrinks/SDCGB.png"},
   new MenuItem{Name="Iced Tea",Category="Sides & Drinks",Price=15,Description="Refreshing house iced tea served cold.",ImagePath="/legacy/SideDrinks/SDIT.png"},
   new MenuItem{Name="Pesto Rice",Category="Sides & Drinks",Price=15,Description="Steamed rice topped with a fragrant basil pesto.",ImagePath="/legacy/SideDrinks/SDPR.png"},
   new MenuItem{Name="Potato Thins",Category="Sides & Drinks",Price=90,Description="Crisp, golden potato slices with a light herb seasoning.",ImagePath="/legacy/SideDrinks/SDPT.png"}
  };

  var menuPaths=menuSeed.Select(x=>x.ImagePath!).ToList();
  var existingMenu=await db.MenuItems.Where(x=>x.ImagePath!=null&&menuPaths.Contains(x.ImagePath)).ToListAsync();
  foreach(var seed in menuSeed){
   var item=existingMenu.FirstOrDefault(x=>x.ImagePath==seed.ImagePath);
   if(item==null){db.MenuItems.Add(seed);continue;}

   // Repair values created by the early five-item placeholder seed. Values changed
   // later in the admin area are deliberately preserved.
   switch(item.ImagePath){
    case "/legacy/FamousOfferings/FOPBR.png":
     if(item.Name=="Crispy Binagoongan")item.Name=seed.Name;
     if(item.Price==295)item.Price=seed.Price;
     break;
    case "/legacy/FamousOfferings/FOSF.png":
     if(item.Price==285)item.Price=seed.Price;
     break;
    case "/legacy/Pasta/PTH.png":
     if(item.Name=="Creamy Pasta")item.Name=seed.Name;
     if(item.Price==245)item.Price=seed.Price;
     break;
    case "/legacy/SideDrinks/SDIT.png":
     if(item.Price==75)item.Price=seed.Price;
     break;
    case "/legacy/SideDrinks/SDPT.png":
     if(item.Name=="Potato Twirls")item.Name=seed.Name;
     if(item.Price==125)item.Price=seed.Price;
     break;
   }
   if(item.Name==seed.Name)item.Description=seed.Description;
  }
  await db.SaveChangesAsync();
 }
}
