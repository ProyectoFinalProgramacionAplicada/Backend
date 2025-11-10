using NetTopologySuite.Geometries;

namespace TruekAppAPI.Services
{
    public interface IGeoService
    {
        Point CreatePoint(double latitude, double longitude);
    }
}