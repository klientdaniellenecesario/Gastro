using Microsoft.AspNetCore.Mvc;

using GastroCebu.Models;

namespace GastroCebu.ViewModels;

public class HomeViewModel
{
    public List<RestaurantListing> FeaturedRestaurants { get; set; } = [];
    public List<DishListing> TrendingDishes { get; set; } = [];
    public List<DishListing> NewDishes { get; set; } = [];
    public List<FoodEvent> UpcomingEvents { get; set; } = [];
    public int RestaurantCount { get; set; }
    public int DishCount { get; set; }
    public int EventCount { get; set; }
    public int UserCount { get; set; }
}