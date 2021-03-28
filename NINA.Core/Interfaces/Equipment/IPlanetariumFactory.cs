using NINA.Core.Enum;

namespace NINA.Model.MyPlanetarium {

    public interface IPlanetariumFactory {

        IPlanetarium GetPlanetarium();

        IPlanetarium GetPlanetarium(PlanetariumEnum planetarium);
    }
}