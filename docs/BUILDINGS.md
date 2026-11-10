# Buildings used in Car For Sale Simulator (Harbour City)

Every building, where it lives, and which GLB dressing it uses (Harbour City build, Phase 14).
"Built-in" = procedural primitive model from `Tools > Car Sales > 1. Check & Install Packages / 2. Build Everything`
(always present, no download needed). "GLB" = Sketchfab model, only if downloaded with
`SKETCHFAB_TOKEN=<token> python3 tools/download_sketchfab_assets.py` — otherwise the Harbour City places its built-in stand-in.

## Master plan (both maps share these zones/sites)

| # | Zone / Site | Building | Source |
|---|-------------|----------|--------|
| 1 | Home – Starter Trailer (`Home_Trailer`, zone 1) | Starter trailer | Built-in |
| 1 | Home – Suburban House (`Home_House`, zone 1) | House | Built-in |
| 1 | Home – Hillside Villa (`Home_Villa`, zone 1) | **Hero villa** → GLB *Modern luxury villa house building home* (zigurat architecture studio, CC BY 4.0, `building_house_01`) — primitive villa kept when GLB missing | Built-in shell + optional GLB |
| 2 | AUTO WORLD HQ dealership (`autoworld`, zone 2) | Dealership showroom + service canopy | Built-in |
| 3 | Auction Center (`AuctionCenter`, zone 3) | Auction hall | Built-in |
| 3 | Used Car Market (`UsedCarMarket`, zone 3) | Open-air used-car lot office | Built-in |
| 4 | Service Centre / MOT (`ServiceCentre`, zone 4) | Service centre | Built-in |
| 5 | Mechanic Workshop (`MechanicWorkshop`, zone 5) | Workshop building | Built-in |
| 6 | Car Wash Center (`CarWashCenter`, zone 6) | Wash hall (GLB *Elephant carWash*, `carwash`) — stand-in when not downloaded | Built-in + optional GLB |
| 7 | Fuel Station (`FuelStation`, zone 7) | Forecourt + convenience box | Built-in |
| 8 | Parts Direct (`PartsDirect`, zone 8) | Parts store | Built-in |
| 8 | Tyres & Wheels (`TyreShop`, zone 8, Harbour City) | Tyre shop workshop | Built-in |
| 9 | Private-seller market (`MapleClose`, `BirchClose`, zone 9) | Residential streets | Built-in |
| 10 | Town Centre (`RentalApartments`, `OfficeTower`, `ParkingGarage`, `PatrolStation`, zone 10) | Apartments, tower, garage, patrol station | Built-in; patrol station → optional GLB *Police station in Kobe, Japan* (`building_patrol`) |
| 11 | Industrial Estate (zone 11) | Industrial sheds | Built-in |
| 13 | Public Park (zone 13) | Statues → GLB (Knabe mit Vögeln, Zwei Bären — CC0) | Built-in + optional GLB |
| 14 | Future Expansion / dealerships (Downtown Motors `dealer_downtown`, Luxury Lane `dealer_luxury`) | Two more dealerships | Built-in |

## Final-Map-only additions (Phase 14)

| POI | Built-in fallback | GLB (when downloaded) |
|-----|------------------|------------------------|
| Patrol Station (`PatrolStation`, zone 10) | White block + blue light bar | *Police station in Kobe, Japan* — `building_patrol` (`building_patrol`) |
| Tyres & Wheels (`TyreShop`, zone 8) | Parked tyre stacks workshop | none — always built-in |
| RentGo Rentals & Sales (`RentGo`, zone 15) | Office + painted lot | Office → GLB *Car showroom* (`Car showroom` key); lot cars → GLBs from `car_hatchback`/`car_sedan`/`car_suv`/`car_sports`/`car_luxury`/`car_pickup` |
| Northgate Offices (zone 15, `NorthgateOffices`) | Dark-glass slab, 15 m | *Headquarters building, office building* (`Headquarters building, office building` key) |
| Offices West (zone 15, `OfficesWest`) | Dark-glass slab, 13 m | *Office building 2* (`Office building 2` key) |
| Northgate Site (zone 14, under construction) | Cranes/hoardings/columns + `Office building 2` as the finished building | *building_construction* for the draft shell; `Office building 2` for the final stage |
| Dockside Site (zone 3, under construction) | Cranes/hoardings/columns + finished building | *Building under construction* (draft); *General Ship Repair* (final — kept deliberately) |
| Hillside Villa (zone 1, `Home_Villa`) | Primitive villa shell | *Modern luxury villa house building home* (hero villa) |
| Southside row (zone 15) | Two offices flanking Business Park Road | see above |
| Park statues (zone 13) | — | *Knabe mit Vögeln* + *Zwei Bären* (CC0) |

## What "used" means here

The Harbour City builder places GLBs **only if the matching file exists** at
`Assets/CarSales/Sketchfab/<category>/<uid>.glb`; anything missing becomes a primitive stand-in (still playable).
Type **Tools → Car Sales → Harbour City → Audit Harbour City** after downloading to see exactly which of the
entries above are live models vs stand-ins.

## Which GLBs are NOT used by the builder

From `docs/HARBOUR_CITY_ASSETS.md`: `two wheeler showroom` (unused), most `car_*` entries used only as RentGo
display cars, four hatchbacks, and three street props — the builder picks the first per key/category.
You can swap any of them by editing `Assets/CarSales/HarbourCity/harbour-city-overrides.json` (skip) and
`harbour-city-assets.json` (different model).
