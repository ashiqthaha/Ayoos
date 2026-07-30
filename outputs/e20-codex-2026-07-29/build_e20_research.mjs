import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { FileBlob, SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = path.dirname(fileURLToPath(import.meta.url));
const agent = "Codex";
const date = "2026-07-29";
const accessed = "2026-07-29";

const filenames = {
  dossier: `E20-Research-Dossier-${agent}-${date}.md`,
  sources: `E20-Sources-${agent}-${date}.md`,
  factCsv: `E20-Fact-Check-${agent}-${date}.csv`,
  factXlsx: `E20-Fact-Check-${agent}-${date}.xlsx`,
  videos: `E20-Trending-Videos-${agent}-${date}.csv`,
  manifest: `E20-Research-Manifest-${agent}-${date}.md`,
};

const sources = [
  {
    id: "GOV-01", group: "Government", title: "Roadmap for Ethanol Blending in India 2020–25",
    org: "NITI Aayog and Ministry of Petroleum and Natural Gas", pub: "2021-06", event: "2021 policy roadmap",
    url: "https://www.niti.gov.in/sites/default/files/2025-07/Roadmap-For-Ethanol-Blending-In-India-2020-25.pdf",
    type: "Policy roadmap", level: "Primary",
    claim: "Phased E20 rollout, vehicle-transition dates, estimated mileage effects, material compatibility, emissions and water-use context.",
    reliability: "High. Foundational government roadmap; projections and policy assumptions should not be mistaken for independent post-rollout outcomes."
  },
  {
    id: "GOV-02", group: "Parliamentary", title: "E20 petrol: safety, performance, complaints and future blend policy",
    org: "Press Information Bureau / Ministry of Road Transport and Highways", pub: "2026-07-23", event: "Parliamentary answer, 2026-07-23",
    url: "https://www.pib.gov.in/PressReleasePage.aspx?PRID=2288271&lang=1&reg=48",
    type: "Parliamentary-answer summary", level: "Primary",
    claim: "Government says no widespread substantiated E20 damage has been reported, gives mileage ranges, and says no decision has been taken beyond E20.",
    reliability: "High for the government’s position; absence of widespread substantiated reports is not proof that no individual failures or costs exist."
  },
  {
    id: "GOV-03", group: "Parliamentary", title: "Ethanol blending programme: current status and E20 questions",
    org: "Press Information Bureau / Ministry of Petroleum and Natural Gas", pub: "2026-07-10", event: "Government clarification, 2026-07-10",
    url: "https://www.pib.gov.in/PressReleasePage.aspx?PRID=2283118&lang=1&reg=3",
    type: "Government Q&A", level: "Primary",
    claim: "Launch chronology, blend status, cost economics, fuel specifications and responses to common consumer claims.",
    reliability: "High for policy facts and official estimates; advocacy framing requires comparison with independent evidence."
  },
  {
    id: "GOV-04", group: "Government", title: "Clarification on misinformation regarding E20 petrol",
    org: "Ministry of Petroleum and Natural Gas / Press Information Bureau", pub: "2026-06-23", event: "Clarification issued 2026-06-23",
    url: "https://www.pib.gov.in/PressReleasePage.aspx?PRID=2277210&lang=2&reg=48",
    type: "Official clarification", level: "Primary",
    claim: "Addresses viral claims involving sugarcane juice, ants, water absorption, mileage and vehicle safety.",
    reliability: "High for the official response; some technical claims need independent corroboration."
  },
  {
    id: "GOV-05", group: "Government", title: "E20 petrol: facts, myths and the government’s implementation case",
    org: "Press Information Bureau", pub: "2026-07-05", event: "Backgrounder issued 2026-07-05",
    url: "https://www.pib.gov.in/PressReleasePage.aspx?PRID=2281287&lang=2&reg=48",
    type: "Government backgrounder", level: "Primary",
    claim: "Summarizes government rebuttals on warranty, insurance, court reporting, water, ants and raw-feedstock misconceptions.",
    reliability: "High for official position; a persuasive backgrounder rather than an independent review."
  },
  {
    id: "GOV-06", group: "Automotive industry", title: "Automotive industry panel on E20 fuel and vehicle performance",
    org: "Press Information Bureau / Ministry of Petroleum and Natural Gas", pub: "2026-07-04", event: "Industry panel, 2026-07-04",
    url: "https://www.pib.gov.in/newsite/erelcontent.aspx?lang=2&reg=48&relid=290866",
    type: "Government-hosted industry panel", level: "Primary/industry testimony",
    claim: "Toyota, Maruti, Hero, TVS, Hyundai, Bajaj and others defended compatibility and reported service experience.",
    reliability: "Useful direct testimony but not a controlled independent fleet study."
  },
  {
    id: "GOV-07", group: "Government", title: "Status of ethanol blending and policy beyond E20",
    org: "Press Information Bureau / Ministry of Petroleum and Natural Gas", pub: "2025-03-18", event: "Policy status through 2025-02",
    url: "https://www.pib.gov.in/PressReleasePage.aspx?PRID=2113234&lang=1&reg=1",
    type: "Official release", level: "Primary",
    claim: "Reports annual blending rates and states that no decision beyond 20 percent had been taken.",
    reliability: "High for official programme statistics."
  },
  {
    id: "GOV-08", group: "Government", title: "India achieves 10 percent ethanol blending ahead of schedule",
    org: "Press Information Bureau", pub: "2022-06-05", event: "E10 achieved 2022-06",
    url: "https://www.pib.gov.in/Pressreleaseshare.aspx?PRID=1831289&lang=2&reg=48",
    type: "Official release", level: "Primary",
    claim: "Documents the E10 milestone and stated import, emissions and farmer-payment benefits.",
    reliability: "High for milestone; savings are official aggregate estimates."
  },
  {
    id: "GOV-09", group: "Government", title: "Amendments to the National Policy on Biofuels, 2018",
    org: "Ministry of Petroleum and Natural Gas / Gazette of India", pub: "2022-06-15", event: "Target amendment, 2022-06",
    url: "https://mopng.gov.in/files/article/articlefiles/Notification-15-06-2022-Amendments-in-NPB-2018.pdf",
    type: "Gazette notification", level: "Primary",
    claim: "Advanced the target for 20 percent ethanol blending from 2030 to ethanol-supply year 2025–26.",
    reliability: "Very high; controlling policy document."
  },
  {
    id: "GOV-10", group: "Government", title: "National Policy on Biofuels and ethanol programme facts",
    org: "Press Information Bureau", pub: "2023-11-29", event: "Programme background through 2023",
    url: "https://www.pib.gov.in/PressReleasePage.aspx?PRID=1982356&lang=2&reg=48",
    type: "Official release", level: "Primary",
    claim: "Feedstocks, procurement policy, blending targets and programme history.",
    reliability: "High for programme design and official statistics."
  },
  {
    id: "GOV-11", group: "Government", title: "Economic Survey 2025–26, Chapter 6: Agriculture and Food Management",
    org: "Government of India, Ministry of Finance", pub: "2026-01", event: "Assessment for FY2025–26",
    url: "https://www.indiabudget.gov.in/economicsurvey/doc/eschapter/echap06.pdf",
    type: "Economic Survey", level: "Primary government analysis",
    claim: "Places biofuel expansion within food, feed, crop-pattern and resource-security trade-offs.",
    reliability: "High; macro-policy analysis, not a vehicle engineering source."
  },
  {
    id: "GOV-12", group: "Government", title: "Frequently asked questions on ethanol-blended petrol",
    org: "Press Information Bureau / Government of India", pub: "2026-07", event: "Current to 2026-07",
    url: "https://www.pib.gov.in/FaqDetails.aspx?ModuleId=4&NoteId=159183&id=159183&lang=1&reg=37",
    type: "Official FAQ", level: "Primary",
    claim: "Government case on cost, safety, mileage, feedstocks, import savings and consumer questions.",
    reliability: "High for stated policy and official estimates; use alongside independent sources."
  },
  {
    id: "GOV-13", group: "Government", title: "E30 gasoline policy and implementation",
    org: "Government of Brazil, Ministry of Mines and Energy", pub: "2025", event: "E30 effective 2025-08-01",
    url: "https://www.gov.br/mme/pt-br/assuntos/secretarias/petroleo-gas-natural-e-biocombustiveis/combustivel-do-futuro/e30/e30",
    type: "Official international policy page", level: "Primary",
    claim: "Brazil moved the standard gasoline blend to E30 within a mature flex-fuel ecosystem.",
    reliability: "Very high for Brazilian policy; fleet and agricultural context differ materially from India."
  },
  {
    id: "GOV-14", group: "Government", title: "E15 fuel registration and vehicle eligibility",
    org: "United States Environmental Protection Agency", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.epa.gov/fuels-registration-reporting-and-compliance-help/e15-fuel-registration",
    type: "Official international technical guidance", level: "Primary",
    claim: "E15 is restricted to eligible light-duty vehicles and excluded from motorcycles and several non-road uses.",
    reliability: "Very high; US regulatory context, useful for labeling and misfueling comparison."
  },
  {
    id: "GOV-15", group: "Research papers", title: "Frequently asked questions related to transportation, air pollution and climate change",
    org: "United States Environmental Protection Agency", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.epa.gov/transportation-air-pollution-and-climate-change/frequent-questions-related-transportation-air",
    type: "Official technical explainer", level: "Primary",
    claim: "Ethanol contains roughly one-third less energy per gallon than gasoline; E10 fuel economy is about 3 percent lower on an energy-adjusted basis.",
    reliability: "Very high for fundamental fuel-energy comparison."
  },
  {
    id: "GOV-16", group: "Government", title: "Fuel prices and fuel products",
    org: "Thailand Ministry of Energy", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.energy.go.th/th/home",
    type: "Official international market page", level: "Primary",
    claim: "Thailand offers differentiated gasoline and gasohol grades, useful for consumer-choice comparison.",
    reliability: "High; product availability varies over time and by station."
  },
  {
    id: "GOV-17", group: "Government", title: "Regulating fuel quality and fuel-quality information standards",
    org: "Australian Government, Department of Climate Change, Energy, the Environment and Water", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.dcceew.gov.au/climate-change/emissions-reduction/regulating-fuel-quality",
    type: "Official international regulation page", level: "Primary",
    claim: "Australia regulates petrol quality and labeling while multiple grades remain on sale.",
    reliability: "Very high for Australian rules."
  },
  {
    id: "GOV-18", group: "Government", title: "Biofuels Act implementation memorandum",
    org: "Philippines Department of Energy", pub: "2009", event: "E10 implementation framework",
    url: "https://legacy.doe.gov.ph/laws-and-issuances/office-president-memorandum-circular-no-184-s-2009",
    type: "Official international policy document", level: "Primary",
    claim: "Philippines E10 implementation provides a regional baseline; later E20 proposals require separate current verification.",
    reliability: "High for the historical E10 framework."
  },
  {
    id: "GOV-19", group: "Government", title: "EU-wide fuel labeling",
    org: "European Commission", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://transport.ec.europa.eu/transport-themes/clean-transport/alternative-fuels-sustainable-mobility-europe/alternative-fuels/eu-wide-fuel-labelling_en",
    type: "Official international regulation explainer", level: "Primary",
    claim: "Common European labels distinguish petrol ethanol grades such as E5, E10 and E85.",
    reliability: "Very high for labeling; actual grade availability differs by member state."
  },
  {
    id: "STD-01", group: "Standards", title: "IS 17021:2018 — E20 fuel specification",
    org: "Bureau of Indian Standards", pub: "2018", event: "Standard current with amendments",
    url: "https://lims.bis.gov.in/home_lab_scope/2175/",
    type: "Fuel standard", level: "Primary",
    claim: "Defines the Indian E20 automotive fuel specification and test framework.",
    reliability: "Very high; standard text access may require BIS navigation."
  },
  {
    id: "STD-02", group: "Standards", title: "Amendment to IS 17021 automotive fuel — E20",
    org: "Bureau of Indian Standards", pub: "2024-12-16", event: "Amendment process 2024",
    url: "https://www.services.bis.gov.in/tmp/PCD4822875_16122024_1.pdf",
    type: "Standards amendment", level: "Primary",
    claim: "Technical amendment context for Indian E20 quality specifications.",
    reliability: "Very high, subject to final/adopted standard status noted in the document."
  },
  {
    id: "RES-01", group: "Research papers", title: "Systematic Evaluation of 20% Ethanol Gasoline Blend (E20) as a Potential Alternate Fuel",
    org: "SAE International / Indian automotive researchers", pub: "2017", event: "Laboratory and field evaluation",
    url: "https://saemobilus.sae.org/papers/systematic-evaluation-20-ethanol-gasoline-blend-e20-a-potential-alternate-fuel-2017-26-0072",
    type: "Peer-reviewed technical paper", level: "Primary research",
    claim: "Evaluates E20 material compatibility, performance, drivability and emissions in Indian vehicles.",
    reliability: "High; test vehicles and conditions may not represent every model or long-term fleet."
  },
  {
    id: "RES-02", group: "Research papers", title: "Legacy Vehicle Fuel System Testing with Intermediate Ethanol Blends",
    org: "National Renewable Energy Laboratory", pub: "2012", event: "Legacy-material test programme",
    url: "https://www.nrel.gov/docs/fy12osti/53606.pdf",
    type: "Government-laboratory report", level: "Primary research",
    claim: "Shows that some older elastomers and metals can be sensitive to intermediate ethanol blends; model year alone is not a reliable proxy.",
    reliability: "High laboratory evidence; US legacy components are not identical to all Indian vehicles."
  },
  {
    id: "RES-03", group: "Research papers", title: "EPAct/V2/E-89 exhaust emissions profiles",
    org: "United States Environmental Protection Agency", pub: "2013", event: "Fuel-effects emissions programme",
    url: "https://gaftp.epa.gov/air/emismod/SPECIATE_supportingdata/v4_4/EPAct_Exhaust_Profiles.pdf",
    type: "Government research dataset/report", level: "Primary research",
    claim: "Intermediate ethanol blends can shift carbonyl emissions, including acetaldehyde.",
    reliability: "High; US fleet and fuel formulations differ from India."
  },
  {
    id: "RES-04", group: "Research papers", title: "Decarbonising India’s transport sector: Navigating trade-offs of biofuel use and electrification",
    org: "Center for Study of Science, Technology and Policy", pub: "2024-12", event: "Scenario analysis through 2030",
    url: "https://cstep.in/drupal/sites/default/files/2024-12/Decarbonising%20India%E2%80%99s%20transport%20sector_Navigating%20trade-offs%20of%20biofuel%20use%20and%20electrification.pdf",
    type: "Independent policy research", level: "Secondary analysis",
    claim: "Quantifies possible land, crop, resource and emissions trade-offs of expanding biofuels.",
    reliability: "High-quality scenario work; results depend on stated assumptions."
  },
  {
    id: "RES-05", group: "Research papers", title: "India’s ethanol roadmap is off course",
    org: "Institute for Energy Economics and Financial Analysis", pub: "2024", event: "Policy critique",
    url: "https://ieefa.org/resources/indias-ethanol-roadmap-course",
    type: "Independent policy analysis", level: "Secondary",
    claim: "Raises land, water, food-security and economic concerns around crop-based ethanol expansion.",
    reliability: "Credible analytical critique; advocacy orientation should be read with government data."
  },
  {
    id: "RES-06", group: "Research papers", title: "Resource, greenhouse-gas and food implications of India’s ethanol pathways",
    org: "Peer-reviewed open-access research", pub: "2026", event: "Scenario study",
    url: "https://pmc.ncbi.nlm.nih.gov/articles/PMC13345272/",
    type: "Peer-reviewed paper", level: "Primary research/modeling",
    claim: "Examines how feedstock choices change water, land, food and lifecycle greenhouse-gas outcomes.",
    reliability: "High, with scenario/model uncertainty."
  },
  {
    id: "RES-07", group: "Research papers", title: "Life-cycle assessment of molasses-based ethanol blends in India",
    org: "Peer-reviewed journal article", pub: "2025", event: "Lifecycle modeling",
    url: "https://www.sciencedirect.com/science/article/pii/S2772826925000732",
    type: "Peer-reviewed paper", level: "Primary research/modeling",
    claim: "Finds possible climate and fossil-energy benefits alongside land, water and toxicity burdens.",
    reliability: "High; results are pathway- and boundary-specific."
  },
  {
    id: "IND-01", group: "Automotive industry", title: "Joint industry statement on E20 compatibility, warranty and insurance",
    org: "SIAM, ARAI and FIPI", pub: "2025-08-30", event: "Joint clarification",
    url: "https://www.siam.in/pressrelease-details.aspx?mpgid=48&pgidtrail=50&pid=585",
    type: "Industry association statement", level: "Primary industry source",
    claim: "Industry bodies reject blanket claims that in-spec E20 automatically voids warranty or insurance.",
    reliability: "High for industry position; contracts and model-specific manuals still govern individual claims."
  },
  {
    id: "MFR-01", group: "Vehicle manufacturers", title: "Tata Motors BS6 Phase II passenger-vehicle range",
    org: "Tata Motors", pub: "2023-02-11", event: "Product transition 2023",
    url: "https://www.tatamotors.com/wp-content/uploads/2023/11/press-11feb23.pdf",
    type: "Manufacturer release", level: "Primary",
    claim: "Tata’s BS6 Phase II passenger range introduced E20-compatible engines.",
    reliability: "High for the stated range and date; legacy models still require manuals."
  },
  {
    id: "MFR-02", group: "Vehicle manufacturers", title: "Hyundai India owner’s manuals",
    org: "Hyundai Motor India", pub: "Model-specific", event: "Current manual library",
    url: "https://www.hyundai.com/in/en/connect-to-service/hyundai-service/owners-manual",
    type: "Owner-manual library", level: "Primary",
    claim: "Model/year-specific fuel requirements and warnings.",
    reliability: "Very high; the exact vehicle manual is more authoritative than a general article."
  },
  {
    id: "MFR-03", group: "Vehicle manufacturers", title: "Hyundai warranty policy",
    org: "Hyundai Motor India", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.hyundai.com/in/en/connect-to-service/warranty-policy/overview",
    type: "Warranty terms", level: "Primary",
    claim: "Improper or insufficient fuel can be excluded; in-spec E20 is not automatically the same as improper fuel.",
    reliability: "Very high for contractual guidance, subject to the warranty booklet for the vehicle."
  },
  {
    id: "MFR-04", group: "Vehicle manufacturers", title: "Toyota India E20 compatibility announcement",
    org: "Toyota Kirloskar Motor", pub: "2026-07-14", event: "Clarification 2026-07-14",
    url: "https://www.toyotabharat.com/announcements/",
    type: "Manufacturer announcement", level: "Primary",
    claim: "Post-2023 vehicles are described as fully E20 material compliant; Toyota also addresses pre-2023 capability.",
    reliability: "High, but owners should retain the exact announcement/manual applicable to their VIN."
  },
  {
    id: "MFR-05", group: "Vehicle manufacturers", title: "Honda Cars India achieves E20 compliance across product range",
    org: "Honda Cars India", pub: "2025-02-06", event: "Compliance announcement 2025",
    url: "https://stage.hondacarindia.com/media/press-releases/achieves-e20-compliance-across-its-product-range-towards-a-sustainable-future",
    type: "Manufacturer release", level: "Primary",
    claim: "Current range certified; India-manufactured cars since 2009 described as materially compatible without parts changes.",
    reliability: "High for Honda Cars India’s statement; certification and optimization dates remain distinct."
  },
  {
    id: "MFR-06", group: "Vehicle manufacturers", title: "Nissan India confirms E20 compatibility and warranty protection",
    org: "Nissan Motor India", pub: "2025-09-16", event: "Compatibility clarification 2025",
    url: "https://www.nissan.in/latest-news/nissan-motor-india-confirms-e20-compatible-vehicles-and-continued-warranty-protection-for-new-nissan-magnite-customers.html",
    type: "Manufacturer release", level: "Primary",
    claim: "Provides variant-specific dates for Magnite engines and warranty language.",
    reliability: "High; applies to specified vehicles and dates."
  },
  {
    id: "MFR-07", group: "Vehicle manufacturers", title: "Volkswagen India E20 compatibility checker/guidance",
    org: "Volkswagen India", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.volkswagen.co.in/app/site/e20-compatibility/",
    type: "Manufacturer guidance", level: "Primary",
    claim: "Volkswagen petrol cars sold after 2020-04-01 are stated to be E20 compatible.",
    reliability: "High; earlier models require direct confirmation rather than negative inference."
  },
  {
    id: "MFR-08", group: "Vehicle manufacturers", title: "E20 fuel explained: is your Škoda compatible?",
    org: "Škoda Auto India", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.skoda-auto.co.in/news/news-detail/e20-fuel-explained-is-your-skoda-compatible-everything-you-need-to-know",
    type: "Manufacturer guidance", level: "Primary",
    claim: "BSVI petrol cars sold after 2020-04-01 are stated to be E20 compatible.",
    reliability: "High; earlier vehicles require a model-specific answer."
  },
  {
    id: "MFR-09", group: "Vehicle manufacturers", title: "Kia India warranty policy",
    org: "Kia India", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.kia.com/in/service/service-and-maintenance/warranty.html",
    type: "Warranty terms", level: "Primary",
    claim: "Improper or insufficient fuel exclusions make exact fuel approval and contamination evidence important.",
    reliability: "Very high for current published terms; the owner booklet controls a specific vehicle."
  },
  {
    id: "MFR-10", group: "Vehicle manufacturers", title: "Kia Carens owner’s manual",
    org: "Kia India", pub: "2024 edition", event: "Model-specific guidance",
    url: "https://www.kia.com/content/dam/kia2/in/en/images/service/service-and-maintenance/owners-manual/Carens_Latest_Manual.pdf",
    type: "Owner manual", level: "Primary",
    claim: "Illustrates why exact manual text is needed rather than a brand-wide inference.",
    reliability: "Very high for the specified model/manual edition."
  },
  {
    id: "MFR-11", group: "Vehicle manufacturers", title: "Hero MotoCorp Sustainability Report FY2023–24",
    org: "Hero MotoCorp", pub: "2024", event: "Fleet status as of 2023-03 onward",
    url: "https://www.heromotocorp.com/content/dam/hero-aem-website/in/en-in/company-section/reports-and-polices/reports/sustainability-report/2023-2024/sustainability-report-fy-2023-24-updated.pdf",
    type: "Manufacturer sustainability report", level: "Primary",
    claim: "Hero describes its models as E20 compatible from March 2023.",
    reliability: "High; owners of older models still need manuals or written confirmation."
  },
  {
    id: "MFR-12", group: "Vehicle manufacturers", title: "Hero’s first flex-fuel motorcycles",
    org: "Hero MotoCorp", pub: "2026-06-03", event: "Product unveiling 2026-06-03",
    url: "https://www.heromotocorp.com/content/dam/hero-aem-website/in/en-in/company-section/press-releases/2026/june-pdf%27s/press_release_hero_motocorp_unveils_its_first_flex_fuel_motorcycles_to_power_indias_self_reliant_mobility_future.pdf",
    type: "Manufacturer release", level: "Primary",
    claim: "Splendor+ and HF Deluxe flex-fuel versions are designed for E20–E85, which is different from ordinary E20 compatibility.",
    reliability: "High for the announced products."
  },
  {
    id: "MFR-13", group: "Vehicle manufacturers", title: "TVS Motor: E20 fuel — all you need to know",
    org: "TVS Motor Company", pub: "Current page", event: "Current access 2026-07-29",
    url: "https://www.tvsmotor.com/media/blog/e20-fuel-all-you-need-to-know",
    type: "Manufacturer technical explainer", level: "Primary industry source",
    claim: "Describes TVS current/BS6 Phase II E20 readiness and practical fuel-system guidance.",
    reliability: "High for TVS’s product position; older models need manual verification."
  },
  {
    id: "MFR-14", group: "Vehicle manufacturers", title: "TVS NTorq 150 owner’s manual",
    org: "TVS Motor Company", pub: "2025-11 edition", event: "Model-specific guidance",
    url: "https://www.tvsmotor.com/-/media/Feature/Owners/25-11-25/TVS-NTORQ-150---TFT.pdf",
    type: "Owner manual", level: "Primary",
    claim: "Approves fuel up to E20 and warns against higher blends and water contamination.",
    reliability: "Very high for the specified model/manual."
  },
  {
    id: "MFR-15", group: "Vehicle manufacturers", title: "Royal Enfield annual report 2022–23",
    org: "Eicher Motors / Royal Enfield", pub: "2023", event: "E20 model transition 2023-04",
    url: "https://www.royalenfield.com/content/dam/eicher-motors/investor/disclosures-under-regulation-46/financial-information/Annual%20Report%20Financial%20Year%202022-23.pdf",
    type: "Manufacturer annual report", level: "Primary",
    claim: "States that all models were updated for E20 from 2023-04-01.",
    reliability: "High for the stated transition; legacy bikes require separate guidance."
  },
  {
    id: "MFR-16", group: "Vehicle manufacturers", title: "Suzuki Motorcycle India media kit: E20-compliant portfolio",
    org: "Suzuki Motorcycle India", pub: "2023-06-05", event: "Portfolio transition 2023",
    url: "https://www.suzukimotorcycle.co.in/media-kit",
    type: "Manufacturer release library", level: "Primary",
    claim: "Suzuki announced its domestic portfolio as E20 compliant.",
    reliability: "High for the announced current portfolio."
  },
  {
    id: "MFR-17", group: "Vehicle manufacturers", title: "Bajaj CT110X owner’s manual",
    org: "Bajaj Auto", pub: "Model-specific", event: "Current model guidance",
    url: "https://cdn.bajajauto.com/-/media/assets/bajajauto/customer-service/owners-manual/owners-manual-pdf/ct110x.pdf",
    type: "Owner manual", level: "Primary",
    claim: "Shows E20 approval for a specified Bajaj model.",
    reliability: "Very high for that manual; not a blanket statement for every historical Bajaj/KTM."
  },
  {
    id: "MFR-18", group: "Vehicle manufacturers", title: "Yamaha R15 V4 product specification",
    org: "India Yamaha Motor", pub: "Current page", event: "Current model",
    url: "https://shop.yamaha-motor-india.com/products/buy-r15-v4-1",
    type: "Manufacturer product page", level: "Primary",
    claim: "Current R15 V4 is advertised as E20 compatible.",
    reliability: "High for current product; not evidence for every older Yamaha."
  },
  {
    id: "MFR-19", group: "Vehicle manufacturers", title: "Renault Duster configurator/specifications",
    org: "Renault India", pub: "Current page", event: "Current model",
    url: "https://www.renault.co.in/cars/renault-duster/configurator.html",
    type: "Manufacturer product page", level: "Primary",
    claim: "Current product information identifies E20 capability.",
    reliability: "High for the displayed model/variant; not a legacy-fleet statement."
  },
  {
    id: "MFR-20", group: "Vehicle manufacturers", title: "Mahindra confirms E20 position for older and newer petrol vehicles",
    org: "Mahindra statement reported by NDTV Auto", pub: "2026-07", event: "Manufacturer clarification 2026-07",
    url: "https://www.ndtv.com/auto/mahindra-confirms-e20-compatibility-for-older-petrol-cars-new-models-perform-better-11732001",
    type: "Reported manufacturer statement", level: "Secondary",
    claim: "Reports Mahindra’s distinction between safe operation of older petrol vehicles and improved performance of E20-optimized newer models.",
    reliability: "Moderate-high; direct Mahindra document was not located in this research pass."
  },
  {
    id: "MFR-21", group: "Vehicle manufacturers", title: "Mercedes-Benz E20 compatibility advisory",
    org: "Mercedes-Benz India statement reported by ET Now", pub: "2026-07-12", event: "Clarification after viral vlog",
    url: "https://www.etnownews.com/auto/mercedes-benz-e20-compatibility-advisory-petrol-bs-vi-certified-auto-maker-responds-to-e20-fuel-efficiency-claims-details-article-155093156/amp",
    type: "Reported manufacturer statement", level: "Secondary",
    claim: "Reports Mercedes-Benz India’s position that its BS VI petrol vehicles are E20 compatible.",
    reliability: "Moderate-high; use the owner manual or written dealer confirmation for a specific VIN."
  },
  {
    id: "MFR-22", group: "Vehicle manufacturers", title: "Honda Motorcycle & Scooter India product information",
    org: "Honda Motorcycle & Scooter India", pub: "Current site", event: "Current access 2026-07-29",
    url: "https://www.honda2wheelersindia.com/",
    type: "Manufacturer product/manual portal", level: "Primary",
    claim: "Current BS6/OBD2B models commonly state E20 compliance; older models require exact manual checking.",
    reliability: "High for product pages; no authoritative blanket legacy statement was located."
  },
  {
    id: "MFR-23", group: "Vehicle manufacturers", title: "MG Motor India owner resources",
    org: "JSW MG Motor India", pub: "Current site", event: "Current access 2026-07-29",
    url: "https://www.mgmotor.co.in/owners",
    type: "Manufacturer owner portal", level: "Primary",
    claim: "Model manuals and service channels are the appropriate source for MG compatibility.",
    reliability: "High as a portal; no reliable brand-wide legacy cut-off was located."
  },
  {
    id: "NEWS-01", group: "News", title: "Centre rejects claim that Supreme Court called E20 an ‘ongoing experiment’",
    org: "The Indian Express", pub: "2026-07-01", event: "Court reporting 2026-06-29; clarification 2026-06-30",
    url: "https://indianexpress.com/article/india/centre-rejects-claim-e20-fuel-push-experiment-10765877/",
    type: "National news", level: "Secondary",
    claim: "Documents the disputed court quote and Attorney General’s clarification.",
    reliability: "High-quality reporting; the formal court order/transcript remains the best legal source."
  },
  {
    id: "NEWS-02", group: "News", title: "YouTuber Sourav Joshi blames E20 for mileage drop; Mercedes responds",
    org: "NDTV Auto", pub: "2026-07-12", event: "Viral vlog and advisory 2026-07-12",
    url: "https://www.ndtv.com/auto/youtuber-sourav-joshi-blames-e20-petrol-for-mileage-drop-mercedes-benz-issues-clarifications-11764630",
    type: "Automotive news", level: "Secondary",
    claim: "Records the viral 17-to-9-to-5 km/l allegation and Mercedes response.",
    reliability: "High for reporting the controversy; the vlog was not a controlled fuel test."
  },
  {
    id: "NEWS-03", group: "News", title: "YouTuber later attributes Mercedes mileage issue to an engine problem",
    org: "Moneycontrol", pub: "2026-07-14", event: "Creator update 2026-07-14",
    url: "https://www.moneycontrol.com/news/trends/youtuber-says-engine-issue-dropped-mileage-in-his-mercedes-benz-suv-after-blaming-e20-petrol-13973788.html",
    type: "National news", level: "Secondary",
    claim: "Documents the creator’s later backtrack, crucial for causal fact-checking.",
    reliability: "High for the reported update; original creator clip should be preserved/verified by the editor if used."
  },
  {
    id: "NEWS-04", group: "News", title: "Kejriwal seeks grievance videos, fuel choice and a price cut",
    org: "India Today", pub: "2026-07-14", event: "Political intervention 2026-07-14",
    url: "https://www.indiatoday.in/india/story/e20-petrol-complaints-kejriwal-seeks-pure-fuel-option-price-cut-2947494-2026-07-14",
    type: "Political/news report", level: "Secondary",
    claim: "Shows political amplification and crowdsourcing of owner complaints.",
    reliability: "High for the statement; crowd submissions are sentiment/anecdotes, not controlled evidence."
  },
  {
    id: "NEWS-05", group: "News", title: "E20 Janta Party goes viral advocating fuel choice",
    org: "Hindustan Times Auto", pub: "2026-07-26", event: "Late-July social-media wave",
    url: "https://auto.hindustantimes.com/auto/news/e20-janta-party-goes-viral-advocating-for-fuel-choice-amid-ethanol-blending-debate-41785146781893.html",
    type: "Automotive/news report", level: "Secondary",
    claim: "Documents the consumer-choice meme/campaign that renewed attention in the final week of July.",
    reliability: "High for trend description; social metrics are volatile."
  },
  {
    id: "NEWS-06", group: "News", title: "E20 Janta Party campaign demands choice of ethanol-free petrol",
    org: "Autocar India", pub: "2026-07-27", event: "Campaign active in late July",
    url: "https://www.autocarindia.com/industry/e20-janta-party-campaign-demands-choice-of-ethanol-free-petrol-440302",
    type: "Automotive industry news", level: "Secondary",
    claim: "Summarizes the campaign’s demands around E0 choice, disclosure and accountability.",
    reliability: "High-quality trade reporting; stated follower counts require timestamping."
  },
  {
    id: "NEWS-07", group: "News", title: "Confusion over two ‘E20 Janta Party’ accounts",
    org: "Business Today", pub: "2026-07-27", event: "Account confusion 2026-07-27",
    url: "https://www.businesstoday.in/india/story/confusion-over-two-e20-janta-parties-on-social-media-which-one-is-actually-real-545490-2026-07-27",
    type: "National news", level: "Secondary",
    claim: "Warns that multiple similarly named social accounts complicate attribution and metric claims.",
    reliability: "High; editors must verify the exact handle before showing a post."
  },
  {
    id: "NEWS-08", group: "News", title: "Petroleum minister acknowledges marginal mileage reduction",
    org: "The Indian Express", pub: "2026-07-04", event: "Ministerial statement 2026-07-02 to 2026-07-04",
    url: "https://indianexpress.com/article/india/e20-petrol-ethanol-reduces-mileage-of-car-bike-hardeep-puri-10769363/",
    type: "National news", level: "Secondary",
    claim: "Reports the government’s acknowledgement of a modest mileage effect, especially for non-optimized vehicles.",
    reliability: "High; exact figures should be anchored to NITI/government primary sources."
  },
  {
    id: "NEWS-09", group: "News", title: "LocalCircles survey reports substantial owner dissatisfaction and mileage complaints",
    org: "Business Standard", pub: "2026-07-05", event: "Survey released 2026-07",
    url: "https://www.business-standard.com/india-news/e20-petrol-rollout-localcircles-survey-mileage-drop-vehicle-owners-126070500287_1.html",
    type: "Survey report/news", level: "Secondary",
    claim: "Captures self-reported consumer sentiment, including many >10 percent mileage-loss reports.",
    reliability: "Useful for sentiment, not causal engineering evidence; self-selection, recall and vehicle-condition confounders apply."
  },
  {
    id: "NEWS-10", group: "News", title: "Raipur consumer commission orders Grand Vitara replacement/refund",
    org: "LiveLaw", pub: "2026-07-16", event: "Commission order 2026-07-14",
    url: "https://www.livelaw.in/consumer-cases/raipur-consumer-commission-maruti-suzuki-grand-vitara-e20-fuel-compatibility-order-541525",
    type: "Legal news", level: "Secondary",
    claim: "Records a district consumer commission order in an E20-related vehicle dispute.",
    reliability: "High-quality legal reporting; one contested district order is not a final nationwide precedent."
  },
  {
    id: "NEWS-11", group: "News", title: "Maruti says it will challenge Raipur order and cites fuel contamination",
    org: "Moneycontrol", pub: "2026-07-17", event: "Company response after 2026-07-14 order",
    url: "https://www.moneycontrol.com/automobile/maruti-suzuki-to-challenge-raipur-consumer-panel-order-directing-grand-vitara-suv-replacement-cites-fuel-contamination-article-13975971.html",
    type: "Automotive/legal news", level: "Secondary",
    claim: "Shows the unresolved causation dispute between in-spec E20 and contaminated fuel.",
    reliability: "High for company response; technical evidence and appeal outcome remain unresolved."
  },
  {
    id: "NEWS-12", group: "News", title: "Government tells Parliament no widespread engine failures from E20",
    org: "NDTV Profit", pub: "2026-07-23", event: "Parliamentary answer 2026-07-23",
    url: "https://www.ndtvprofit.com/india/e20-petrol-has-not-caused-widespread-engine-failures-or-vehicle-issues-govt-tells-parliament-11796432",
    type: "National news", level: "Secondary",
    claim: "Independent news rendering of the government’s current parliamentary position.",
    reliability: "High; use GOV-02 for the primary answer."
  },
  {
    id: "NEWS-13", group: "News", title: "Parliament told mileage can fall 3–5 percent in some older-design vehicles",
    org: "The Times of India", pub: "2026-07-23", event: "Parliamentary answer 2026-07-23",
    url: "https://timesofindia.indiatimes.com/india/e20-petrol-cuts-mileage-by-up-to-5-in-older-vehicles-govt-tells-parliament/articleshow/132530985.cms",
    type: "National news", level: "Secondary",
    claim: "Highlights the consumer-facing mileage caveat within the current official position.",
    reliability: "High; cross-check with GOV-02 and GOV-01."
  },
  {
    id: "NEWS-14", group: "News", title: "Why E20 may not be cheaper at the pump",
    org: "Business Standard", pub: "2026-07-10", event: "Government cost clarification 2026-07-10",
    url: "https://www.business-standard.com/india-news/e20-petrol-price-ethanol-blending-govt-explains-126071000435_1.html",
    type: "Business news", level: "Secondary",
    claim: "Explains current ethanol procurement economics and why the consumer may not see a discount.",
    reliability: "High for reported official numbers; retail-pricing components vary by state and policy."
  },
  {
    id: "NEWS-15", group: "News", title: "Ethanol economics explained: why E20 may cost consumers more per kilometre",
    org: "The Financial Express", pub: "2026-07", event: "Current debate",
    url: "https://www.financialexpress.com/policy/economy-ethanol-economics-explained-why-does-made-in-india-e20-fuel-cost-you-more-4289595/lite/",
    type: "Economic analysis", level: "Secondary",
    claim: "Separates blend-component cost, taxation, retail price and lower energy per litre.",
    reliability: "Credible analysis; verify changing procurement and crude-price assumptions."
  },
  {
    id: "NEWS-16", group: "News", title: "E20 debate in Kerala: mileage, damage and consumer questions",
    org: "Onmanorama", pub: "2026-07-12", event: "Explainer published 2026-07-12",
    url: "https://www.onmanorama.com/videos/news/news-beyond-kerala/2026/07/12/is-e-20-petrol-reducing-mileage-and-damaging-vehicles-onmanorama-explainer.html",
    type: "Malayalam/regional video news", level: "Secondary",
    claim: "Evidence of Malayalam-audience interest and the dominant local framing.",
    reliability: "High for media relevance; technical claims should be cross-checked."
  },
  {
    id: "NEWS-17", group: "News", title: "Bajaj says recent motorcycles are E20 compatible",
    org: "BikeDekho", pub: "2026-07", event: "Company clarification 2026-07",
    url: "https://www.bikedekho.com/news/category-industry-news/bajaj-claims-all-its-bikes-sold-over-the-last-10-years-are-e20-compatible-19792",
    type: "Automotive news", level: "Secondary",
    claim: "Reports a broad Bajaj compatibility statement, sometimes extended in coverage to allied brands.",
    reliability: "Moderate; no direct manufacturer release was captured, so exact models and KTM coverage need human verification."
  },
  {
    id: "NEWS-18", group: "News", title: "Royal Enfield launches E20 retrofit kits for select BS3/BS4 bikes",
    org: "Team-BHP", pub: "2026", event: "Reported retrofit availability",
    url: "https://www.team-bhp.com/news/royal-enfield-launches-e20-retrofit-kits-bs3-bs4-bikes",
    type: "Automotive community news", level: "Secondary",
    claim: "Reports model-specific retrofit options for some older motorcycles.",
    reliability: "Moderate; availability, approved models, parts and warranty must be verified with Royal Enfield."
  },
  {
    id: "NEWS-19", group: "News", title: "India’s ethanol programme brings gains and resource questions",
    org: "Associated Press", pub: "2025", event: "Programme assessment",
    url: "https://apnews.com/article/069776a758cd22d037866b8701df6d9a",
    type: "International news", level: "Secondary",
    claim: "Balanced reporting on farmer, import, climate, land and water trade-offs.",
    reliability: "High-quality journalism; individual case studies are illustrative."
  },
  {
    id: "VID-01", group: "Videos", title: "E20 Petrol Issues & Mileage Drop | This Fuel Conditioner Will Improve Mileage By Upto 20%",
    org: "Mechanical Tech Hindi", pub: "2026-02-18", event: "Video upload 2026-02-18",
    url: "https://www.youtube.com/watch?v=VBn7pf8rnMk",
    type: "YouTube video", level: "Primary media object",
    claim: "Promotes a fuel-conditioner solution alongside mileage/damage framing.",
    reliability: "Low for the additive performance claim unless independent test evidence is supplied; useful reaction target."
  },
  {
    id: "VID-02", group: "Videos", title: "India’s E20 Petrol Rollout – Latest Update!",
    org: "MotorBeam", pub: "2025-09-12", event: "Video upload 2025-09-12",
    url: "https://www.youtube.com/watch?v=i7XMrb8sRYc",
    type: "YouTube video", level: "Primary media object",
    claim: "Automotive-channel explanation of rollout and compatibility.",
    reliability: "Moderate; useful for framing and audience questions, not a substitute for official documents."
  },
  {
    id: "VID-03", group: "Videos", title: "E20 petrol Malayalam explainer",
    org: "Asianet News Malayalam", pub: "2025-08-28", event: "Video upload 2025-08-28",
    url: "https://www.youtube.com/watch?v=Q8xX66oEvws",
    type: "YouTube video", level: "Primary media object",
    claim: "A high-reach Malayalam framing of mileage and vehicle concerns.",
    reliability: "Useful for audience/media analysis; technical details require independent checking."
  },
  {
    id: "VID-04", group: "Videos", title: "E20 petrol mandatory from April 2026",
    org: "Kanak News", pub: "2026-02-28", event: "Video upload 2026-02-28",
    url: "https://www.youtube.com/watch?v=Nb1xncbCOQw",
    type: "YouTube video", level: "Primary media object",
    claim: "News framing of the April 2026 nationwide E20/RON 95 requirement.",
    reliability: "Moderate; notification wording should be checked against primary documents."
  },
  {
    id: "VID-05", group: "Videos", title: "E20 Petrol Explained: 10 Biggest Myths vs Facts",
    org: "NDTV", pub: "2026-07-07", event: "Video upload 2026-07-07",
    url: "https://www.ndtv.com/video/e20-petrol-explained-10-biggest-myths-vs-facts-1124139",
    type: "News video", level: "Secondary media",
    claim: "Recent myth-versus-fact package with strong official framing.",
    reliability: "Credible newsroom source; evaluate omissions and evidence balance."
  },
  {
    id: "VID-06", group: "Videos", title: "E20 fuel: all you need to know",
    org: "NDTV", pub: "2025", event: "Video upload before current controversy",
    url: "https://www.ndtv.com/video/e20-fuel-all-you-need-to-know-about-e20-petrol-in-india-what-is-e20-fuel-979801",
    type: "News video", level: "Secondary media",
    claim: "Baseline explainer useful for comparing pre-controversy and current framing.",
    reliability: "Credible general explainer; not current trend evidence by itself."
  },
  {
    id: "VID-07", group: "Videos", title: "E20 Petrol in India: Pros, Cons and What It Means for Your Vehicle",
    org: "The Indian Express", pub: "2026", event: "Video published during current debate",
    url: "https://indianexpress.com/videos/news-video/e20-petrol-in-india-pros-cons-and-what-it-means-for-your-vehicle-e20-controversy/",
    type: "News video", level: "Secondary media",
    claim: "Balanced national explainer on policy and consumer impact.",
    reliability: "High for editorial context; technical figures need primary sourcing."
  },
  {
    id: "VID-08", group: "Videos", title: "India’s E20 fuel debate explained: facts, myths and the road to E100",
    org: "The Times of India", pub: "2026-07", event: "Current debate",
    url: "https://timesofindia.indiatimes.com/videos/explainers/indias-e20-fuel-debate-explained-facts-myths-the-road-to-e100/amp_videoshow/132288568.cms",
    type: "News video", level: "Secondary media",
    claim: "Connects current controversy to future higher-blend speculation.",
    reliability: "Moderate-high; E100/future policy must not be presented as decided."
  },
  {
    id: "VID-09", group: "Videos", title: "E20 fuel debate explained in five minutes: keeping a car or bike safe",
    org: "Mint", pub: "2026-07-27", event: "Video published 2026-07-27",
    url: "https://www.livemint.com/videos/e20-fuel-debate-explained-in-5-minutes-how-to-ensure-your-car-or-bike-stays-safe-11755609513664.html",
    type: "News video", level: "Secondary media",
    claim: "Very recent consumer-practical explainer.",
    reliability: "Credible; condensed format may blur compatibility, optimization and contamination."
  },
  {
    id: "VID-10", group: "Videos", title: "Viral E20 claims trigger government and industry response",
    org: "The Economic Times", pub: "2026-07-04", event: "Government/industry event 2026-07-04",
    url: "https://economictimes.indiatimes.com/news/videos/viral-e20-claims-trigger-govt-response-toyota-and-industry-experts-also-issue-clarification/videoshow/132184285.cms",
    type: "News video", level: "Secondary media",
    claim: "Captures the coordinated official/industry rebuttal phase.",
    reliability: "High for event coverage; participants are interested parties."
  },
  {
    id: "SOC-01", group: "Social-media sentiment", title: "Kerala Reddit discussion: E20, consumer choice and vehicle risk",
    org: "r/Kerala", pub: "2026-07-28", event: "Discussion active 2026-07-28",
    url: "https://www.reddit.com/r/Kerala/comments/1v8pz76/",
    type: "Public forum thread", level: "Primary sentiment only",
    claim: "Shows high-engagement Kerala concerns about choice, testing, price and older vehicles.",
    reliability: "Useful for questions and sentiment; not technical proof. Thread URL may require title search if the slug changes."
  },
  {
    id: "SOC-02", group: "Social-media sentiment", title: "KeralaSpeaks discussion on E20 rollout",
    org: "r/KeralaSpeaks", pub: "2026-07-15", event: "Discussion active 2026-07-15",
    url: "https://www.reddit.com/r/KeralaSpeaks/comments/1ux8wyp/",
    type: "Public forum thread", level: "Primary sentiment only",
    claim: "Captures local political and consumer reactions during the mid-July wave.",
    reliability: "Sentiment evidence only; user identities and claims are unverified."
  }
];

const factRows = [
  ["E20 destroys every old engine.", "False", "Risk is vehicle- and material-specific. Some legacy hoses, seals, metals or carburetor calibrations can be unsuitable, but universal destruction is contradicted by controlled testing, manufacturer guidance and fleet experience.", "High", "GOV-01; RES-01; RES-02", "GOV-02; IND-01", "An individual old vehicle can still fail; model/VIN, condition, storage and fuel quality matter.", date, agent],
  ["E20 reduces mileage by exactly 20%.", "False", "The blend’s lower energy content creates a physical penalty, but official Indian estimates are generally about 1–2 percent for E20-designed/E20-calibrated four-wheelers, 3–4 percent for some two-wheelers and 6–7 percent for older E0-designed/E10-calibrated four-wheelers. Real use varies.", "High", "GOV-01; GOV-15", "NEWS-08; NEWS-13", "E10-to-E20 loss is smaller than E0-to-E20; uncontrolled owner reports may include other faults.", date, agent],
  ["E20 causes immediate corrosion.", "False", "In-spec E20 does not immediately corrode a compatible, intact fuel system. Risk rises with incompatible legacy materials, water contamination, phase separation, long storage or fuel outside specification.", "High", "STD-01; RES-02; MFR-14", "GOV-04; GOV-05", "Corrosion can occur in a vulnerable system; 'not immediate for all' is not 'impossible'.", date, agent],
  ["Every vehicle manufactured after a single cut-off year is E20 compatible.", "Misleading", "Manufacturers adopted materials, certification and calibration on different schedules. BS stage and year are useful clues, not universal proof.", "High", "MFR-01; MFR-05; MFR-06; MFR-07; MFR-08", "GOV-01", "The owner manual, fuel-cap label, VIN-specific bulletin or written OEM answer controls.", date, agent],
  ["E20 is cheaper than ordinary petrol for the consumer.", "Misleading", "Domestic ethanol can reduce crude imports, but current procurement, taxes, logistics and administered retail pricing do not guarantee a pump discount. Lower energy per litre can also raise cost per kilometre.", "High", "GOV-03; GOV-12", "NEWS-14; NEWS-15", "Relative economics change with crude, feedstock, tax and state-price assumptions.", date, agent],
  ["E20 is environmentally harmless.", "False", "E20 can reduce some tailpipe CO/HC and fossil-petrol use, but it may raise acetaldehyde and its lifecycle footprint depends on feedstock, fertilizer, irrigation, distillation energy, transport and land use.", "High", "GOV-01; RES-03; RES-06; RES-07", "RES-04; RES-05", "Tailpipe and full lifecycle boundaries can yield different answers.", date, agent],
  ["E20 eliminates India’s oil imports.", "False", "E20 displaces only part of the petrol pool and India still imports crude for the remaining gasoline and many other petroleum products.", "High", "GOV-08; GOV-10", "NEWS-19", "Gross foreign-exchange savings are real policy benefits but do not equal energy independence.", date, agent],
  ["Ethanol in Indian petrol always comes from sugarcane.", "False", "Permitted feedstocks include sugarcane juice/syrup, molasses, maize, damaged grains, surplus rice and second-generation residues.", "High", "GOV-09; GOV-10; GOV-12", "RES-04; RES-06", "The actual monthly feedstock mix is not visible at the fuel pump and can change.", date, agent],
  ["Using E20 voids every vehicle warranty.", "False", "Industry bodies and several manufacturers say in-spec E20 does not automatically void warranty. Claims can still be rejected for unapproved fuel, excessive ethanol, contamination, misuse or an ineligible model.", "High", "IND-01; MFR-03; MFR-06; MFR-09", "GOV-05", "Contract wording, approved blend, causation and service evidence decide an individual claim.", date, agent],
  ["Premium petrol does not contain ethanol.", "False", "Premium or high-octane branding describes octane and additives, not necessarily ethanol content. Only a grade explicitly specified and labeled E0 is ethanol-free.", "Moderate", "STD-01; GOV-03", "GOV-04", "Brand formulations and station supply can change; ask the OMC for the product specification.", date, agent],
  ["Consumers can easily choose E10 instead of E20 across India.", "False", "The normal nationwide petrol pool is now E20-led, and E10/E0 is not a consistently available parallel choice at ordinary stations.", "High", "GOV-02; GOV-03", "NEWS-05; NEWS-06", "Specialized fuels may exist locally, but availability, blend and price must be verified.", date, agent],
  ["E20 gives every engine more power because ethanol has higher octane.", "Misleading", "Higher octane resists knock; it is not additional energy. An engine must have suitable compression, ignition and fueling calibration to exploit the octane benefit.", "High", "GOV-01; GOV-15", "GOV-04", "A calibrated engine may maintain or improve knock-limited performance while still consuming more litres.", date, agent],
  ["A fuel additive can make every old vehicle E20-safe.", "Unsupported", "An additive cannot universally change incompatible hoses, seals, metals, tank coatings, carburetor jetting or OEM approval. No independent evidence supports a universal retrofit-in-a-bottle claim.", "Moderate-high", "RES-02; MFR-14", "VID-01", "A specific additive may have a narrow tested function; demand model-specific independent data and OEM approval.", date, agent],
  ["Leaving E20 in any parked vehicle always damages the fuel system.", "False", "Compatible sealed systems can store in-spec fuel without inevitable damage. Risk rises with long storage, humid air exchange, water ingress, pre-existing contamination and legacy materials.", "High", "RES-02; MFR-14", "GOV-04", "There is no universal safe-duration number; follow the exact owner manual and storage instructions.", date, agent],
  ["One litre of ethanol always consumes exactly 10,000 litres of water.", "Misleading", "Water figures vary drastically by feedstock, region and accounting boundary. Agricultural water footprint, irrigation withdrawals and distillery process water are different metrics.", "High", "GOV-01; GOV-11; RES-06", "RES-04; RES-05", "Quote the crop, location, year and system boundary whenever using a water number.", date, agent],
  ["Ants around a fuel cap prove that petrol contains raw sugar.", "False", "Fuel ethanol is a distilled, anhydrous chemical; it is not sugarcane juice and does not contain fermentable sugar in normal specification.", "High", "STD-01; GOV-04; GOV-05", "", "Ants near a vehicle can have unrelated causes; a fuel-quality complaint requires sampling and testing.", date, agent],
  ["The Government admitted in the Supreme Court that E20 is an uncontrolled experiment.", "False", "The Attorney General’s office disputed the widely circulated wording and said the statement was misreported. The viral quote should not be repeated as a settled court finding.", "Moderate-high", "GOV-05", "NEWS-01", "The editor should inspect the formal order/transcript before using direct quotation.", date, agent],
  ["All BS6 petrol vehicles are automatically E20 compatible.", "Misleading", "BS6 is an emissions regime, not by itself a fuel-material certificate. Several brands specify 2020, 2023, 2024 or 2025 transitions, and older BS6 models may need a manual-specific answer.", "High", "MFR-01; MFR-05; MFR-06; MFR-07; MFR-08", "IND-01", "Some manufacturers do make broader statements; use the relevant OEM document.", date, agent],
  ["E20-compatible and E20-optimized mean the same thing.", "False", "Compatibility means materials and operation can tolerate the fuel; optimization adds calibration/design to recover efficiency, starting, emissions or performance.", "High", "GOV-01; MFR-20", "GOV-02", "Certification/homologation is a third distinct question.", date, agent],
  ["E20 causes no mileage loss.", "Misleading", "Lower energy per litre makes some volumetric fuel-economy reduction expected unless engine design/calibration recovers it. Government and NITI estimates acknowledge a range.", "High", "GOV-01; GOV-15", "NEWS-08; NEWS-13", "Some optimized vehicles or noisy real-world tests may show little observable difference.", date, agent],
  ["No widespread substantiated failure reports prove that no individual E20-related failure can occur.", "Misleading", "Population-level surveillance and individual causation are different. A specific failure can involve incompatible parts, excessive blend, water or contamination even if nationwide catastrophic claims are false.", "High", "GOV-02; STD-01; RES-02", "NEWS-10; NEWS-11", "Independent failure diagnosis requires fuel sampling, part inspection and chain-of-custody evidence.", date, agent],
  ["E20 is simply adulterated petrol.", "Misleading", "E20 supplied to the notified BIS specification is a legal standardized automotive fuel, not illicit adulteration.", "High", "STD-01; GOV-03", "GOV-04", "Fuel outside the specified ethanol range or contaminated with water can still be non-compliant.", date, agent],
  ["A fall from 17 km/l to 5 km/l can be explained by E20 energy content alone.", "False", "That roughly 70 percent drop is far larger than the fuel-energy difference. In the widely reported viral case, the creator later attributed the problem to an engine issue.", "High", "GOV-01; GOV-15", "NEWS-02; NEWS-03", "A vehicle fault and fuel quality should be diagnosed before assigning causation.", date, agent],
  ["E20 absorbs water directly through any sealed tank until it separates.", "False", "Ethanol is hygroscopic, but a sound sealed fuel system does not freely absorb unlimited atmospheric moisture. Phase separation requires enough water and depends on temperature and fuel composition.", "High", "RES-02; MFR-14", "GOV-04", "Water can enter through poor storage, leaks, station contamination or repeated humid-air exchange.", date, agent],
  ["Brazil proves India can adopt higher blends with no consumer trade-offs.", "Misleading", "Brazil’s E30 gasoline exists alongside decades of flex-fuel deployment and hydrous-ethanol choice, with different fleet, feedstock and distribution conditions.", "High", "GOV-13", "GOV-14; GOV-19", "Brazil supplies useful lessons but is not a directly interchangeable policy model.", date, agent]
];

const videoRows = [
  ["E20 Petrol Issues & Mileage Drop | This Fuel Conditioner Will Improve Mileage By Upto 20%", "Mechanical Tech Hindi", "https://www.youtube.com/watch?v=VBn7pf8rnMk", "2026-02-18", "13,114", "1,570,000", "0.84%", "Not reliably visible", "Hindi", "Title-derived: E20 issues / mileage / up to 20%", "Problem-solution hook: E20 mileage anxiety followed by an additive promise", "A fuel conditioner can recover up to 20% mileage and address E20 issues", "Presenter demonstration/claims; no controlled independent dataset identified", "No model-specific compatibility proof; no blinded A/B fuel test; additive limitations", "Mileage loss, old vehicles, quick fixes, trust", "High", "Challenge the universal additive claim; optional clip, use short quotation/screenshot with critique", agent],
  ["India’s E20 Petrol Rollout – Latest Update!", "MotorBeam", "https://www.youtube.com/watch?v=i7XMrb8sRYc", "2025-09-12", "30,658", "1,210,000", "2.53%", "Not reliably visible", "English/Hindi", "Title-derived: E20 rollout update", "Urgent update for owners", "Explains rollout, compatibility and consumer implications", "Automotive reporting and policy references", "Pre-dates the June–July 2026 controversy and latest nationwide/default phase", "Compatibility, mileage, old vehicles", "Medium", "Reference for historical framing; not the primary current reaction target", agent],
  ["ഇ 20 പെട്രോൾ / E20 petrol Malayalam explainer", "Asianet News Malayalam", "https://www.youtube.com/watch?v=Q8xX66oEvws", "2025-08-28", "133,167", "11,500,000", "1.16%", "Not reliably visible", "Malayalam", "Title-derived: E20 petrol", "Direct consumer warning/explainer framing", "Explains E20 and vehicle/mileage questions", "News presentation; source details not fully captured", "Compatibility versus optimization and current 2026 developments", "Malayalam concern about mileage and older vehicles", "High", "Reference as existing Malayalam coverage; Para Cast should add evidence hierarchy and current update", agent],
  ["E20 petrol mandatory from April 2026", "Kanak News", "https://www.youtube.com/watch?v=Nb1xncbCOQw", "2026-02-28", "50,399", "8,850,000", "0.57%", "Not reliably visible", "Odia", "Title-derived: mandatory from April 1", "Deadline/mandate hook", "Nationwide E20/RON 95 requirement begins April 2026", "News report on government notification", "Exact notification exceptions, vehicle-transition distinction and consumer cost", "Deadline anxiety, availability, old vehicles", "Medium", "Reference policy deadline only after verifying the notification text", agent],
  ["E20 Petrol Explained: 10 Biggest Myths vs Facts", "NDTV", "https://www.ndtv.com/video/e20-petrol-explained-10-biggest-myths-vs-facts-1124139", "2026-07-07", "Not reliably visible", "Not reliably visible", "", "6:01", "English/Hindi", "Myths vs facts", "Rapid myth-busting", "Most viral E20 claims are exaggerated and in-spec fuel is safe", "Government and automotive expert statements", "Consumer choice, contract language, lifecycle trade-offs, individual diagnosis", "Safety, mileage, warranty, distrust", "High", "Respond constructively: keep correct debunks, add missing consumer-cost and evidence caveats", agent],
  ["E20 fuel: all you need to know about E20 petrol in India", "NDTV", "https://www.ndtv.com/video/e20-fuel-all-you-need-to-know-about-e20-petrol-in-india-what-is-e20-fuel-979801", "2025 (exact date not captured)", "Not reliably visible", "Not reliably visible", "", "Not reliably visible", "English/Hindi", "E20 all you need to know", "Beginner explainer", "Defines E20 and policy benefits/risks", "News research and expert commentary", "Latest 2026 trigger and late-July consumer-choice campaign", "Basic compatibility and mileage questions", "Low-medium", "Use as baseline only; current package should not react mainly to an older explainer", agent],
  ["E20 Petrol in India: Pros, Cons and What It Means for Your Vehicle", "The Indian Express", "https://indianexpress.com/videos/news-video/e20-petrol-in-india-pros-cons-and-what-it-means-for-your-vehicle-e20-controversy/", "2026 (exact date not captured)", "Not reliably visible", "Not reliably visible", "", "Not reliably visible", "English", "Pros / cons / your vehicle", "Balanced impact question", "E20 has national benefits and vehicle-specific trade-offs", "News reporting and official sources", "Model-by-model manual workflow and Kerala practicalities", "Mileage, damage, policy", "Medium-high", "Reference for balance; Para Cast can add Malayalam, two-wheeler and compatibility workflow", agent],
  ["India’s E20 Fuel Debate Explained: Facts, Myths & The Road To E100", "The Times of India", "https://timesofindia.indiatimes.com/videos/explainers/indias-e20-fuel-debate-explained-facts-myths-the-road-to-e100/amp_videoshow/132288568.cms", "2026-07 (exact day not captured)", "Not reliably visible", "Not reliably visible", "", "Not reliably visible", "English/Hindi", "Facts, myths, road to E100", "Future-fuel escalation hook", "Frames E20 as one stage toward higher blends", "News reporting and policy discussion", "Government says no decision beyond E20; E100 needs flex-fuel vehicles", "Future mandates, fear, compatibility", "High", "Challenge any implication that E100 is already decided; optional clip", agent],
  ["E20 Fuel Debate Explained in 5 Minutes: How To Ensure Your Car or Bike Stays Safe", "Mint", "https://www.livemint.com/videos/e20-fuel-debate-explained-in-5-minutes-how-to-ensure-your-car-or-bike-stays-safe-11755609513664.html", "2026-07-27", "Not reliably visible", "Not reliably visible", "", "5:00 (title-derived)", "English/Hindi", "E20 debate in 5 minutes / keep vehicle safe", "Urgent practical-safety hook", "Owners can reduce risk with compatibility and maintenance checks", "Recent reporting and expert guidance", "No universal storage interval; warranty evidence and fuel sampling workflow", "What should I do now?", "High", "Complement rather than attack; add an evidence-backed Kerala checklist", agent],
  ["Viral E20 Claims Trigger Govt Response; Toyota and Industry Experts Clarify", "The Economic Times", "https://economictimes.indiatimes.com/news/videos/viral-e20-claims-trigger-govt-response-toyota-and-industry-experts-also-issue-clarification/videoshow/132184285.cms", "2026-07-04", "Not reliably visible", "Not reliably visible", "", "Not reliably visible", "English/Hindi", "Viral claims / government response", "Authority-response hook", "Industry experience contradicts catastrophic E20 claims", "Government panel and manufacturer testimony", "Industry participants’ interests; no independent long-duration fleet comparison", "Trust in government vs owners", "High", "Use clips as official-side evidence, then test the strongest claims independently", agent],
  ["Is E20 petrol reducing mileage and damaging vehicles?", "Onmanorama", "https://www.onmanorama.com/videos/news/news-beyond-kerala/2026/07/12/is-e-20-petrol-reducing-mileage-and-damaging-vehicles-onmanorama-explainer.html", "2026-07-12", "Not reliably visible", "Not reliably visible", "", "Not reliably visible", "Malayalam/English", "Question-derived: mileage and damage", "Direct Malayalam owner concern", "Explains whether E20 causes mileage loss or damage", "News explainer and cited official context", "Consumer-choice economics, certification vs optimization, individual evidence chain", "Malayali owner anxiety, mileage, old vehicles", "Very high", "Best Malayalam comparator; respond by adding the missing distinction and late-July update", agent],
  ["Race with Brothers: Kon Jeetega (reported E20 mileage segment)", "Sourav Joshi Vlogs", "Original stable URL not captured; reported in https://www.ndtv.com/auto/youtuber-sourav-joshi-blames-e20-petrol-for-mileage-drop-mercedes-benz-issues-clarifications-11764630", "2026-07-12", "Not reliably visible", "41,300,000 (reported)", "", "Not reliably visible", "Hindi", "Original thumbnail not captured", "Personal luxury-SUV mileage shock: 17 to 9 to 5 km/l", "E20 caused an extreme Mercedes mileage collapse", "Dashboard observation and personal attribution", "No fuel A/B control, diagnostic data or energy-content plausibility; later engine-issue backtrack", "Strong anger, confirmation bias, arguments over E20", "Very high", "Primary reaction claim, but use only rights-cleared short excerpts or news screenshots; include the 2026-07-14 correction", agent]
];

function csvEscape(value) {
  const s = value === null || value === undefined ? "" : String(value);
  return `"${s.replaceAll('"', '""')}"`;
}

function toCsv(headers, rows) {
  return "\uFEFF" + [headers, ...rows].map(row => row.map(csvEscape).join(",")).join("\r\n") + "\r\n";
}

const sourceLibrary = `# E20 Petrol in India — Source Library

**Agent:** ${agent}  
**Research cutoff:** ${date}  
**Access date for all links:** ${accessed}  
**Method:** Sources are grouped by the assignment taxonomy, deduplicated by canonical URL, and marked primary/secondary. Publication date and event date are recorded separately where they differ. Social posts are used only for sentiment.

## Reliability scale

- **Very high / High:** controlling document, official manual, standard, transparent research paper, or well-sourced direct statement.
- **Moderate-high / Moderate:** reputable secondary reporting or a company statement reported by a third party.
- **Low for proof:** useful as a media object or sentiment sample, not as scientific evidence.

${["Government", "Parliamentary", "Vehicle manufacturers", "Oil companies", "Research papers", "Standards", "Automotive industry", "News", "Videos", "Social-media sentiment"].map(group => {
  const items = sources.filter(s => s.group === group);
  if (group === "Oil companies" && items.length === 0) {
    return `## ${group}\n\nNo sufficiently specific, current, primary OMC document was captured in this pass. Fuel-quality/specification claims are therefore anchored to BIS and MoPNG/PIB materials. Station-specific blend and labeling should be verified with the supplying OMC before publication.\n`;
  }
  return `## ${group}\n\n${items.map(s => `### ${s.id} — ${s.title}

- **Organization/author:** ${s.org}
- **Publication date:** ${s.pub}
- **Event date:** ${s.event}
- **URL:** ${s.url}
- **Source type:** ${s.type}
- **Primary/secondary:** ${s.level}
- **Key claim supported / why it matters:** ${s.claim}
- **Reliability assessment:** ${s.reliability}
- **Access date:** ${accessed}
`).join("\n")}`;
}).join("\n")}

## Source-control notes

1. Government releases are authoritative about policy and the government’s own figures, but they are not treated as independent evaluation of consumer outcomes.
2. Manufacturer statements establish that manufacturer’s position; the exact owner manual, VIN, fuel-cap label and warranty booklet remain decisive.
3. News articles are used to reconstruct the June–July 2026 event sequence and contested claims. When a primary source exists, the dossier cites both.
4. YouTube views/subscribers are timestamped only where a search result exposed them. Blank or “not reliably visible” fields were not estimated.
5. Exact Google Trends values were not captured because the live endpoint returned HTTP 429 and the authenticated browser bridge was unavailable. No normalized search index, state ranking or breakout label is invented.
`;

const dossier = `# E20 Petrol in India — Research Dossier

**Agent:** ${agent}  
**Research date/cutoff:** ${date}  
**Audience:** Para Cast editors and Malayalam explainer producers  
**Scope:** Research package, not a narration or finished script  
**Citation system:** Bracketed IDs refer to the companion source library.

---

## 1. Executive summary

E20 is petrol blended with approximately 20 percent anhydrous ethanol by volume and supplied under an Indian fuel specification. India accelerated the blend to reduce exposure to crude imports, create a domestic market for agricultural and residue feedstocks, support rural income, improve octane, and reduce selected tailpipe pollutants. The programme moved from E10 in 2022 to phased E20 availability from 2023, vehicle-material transitions, and a nationwide E20/RON 95 default from 1 April 2026. [GOV-01] [GOV-02] [GOV-09] [STD-01]

The present discussion is not traceable to one announcement. It is a cascading controversy: viral “sugar in fuel,” ants and engine-damage claims prompted a government rebuttal on 23 June; a disputed Supreme Court “experiment” quote circulated after 29 June; officials and manufacturers answered in early July; a 12 July Sourav Joshi vlog blamed E20 for an extreme Mercedes mileage collapse before the creator later cited an engine issue; political and legal disputes followed; and the late-July “E20 Janta Party” campaign turned technical anxiety into a consumer-choice movement. [GOV-04] [NEWS-01] [NEWS-02] [NEWS-03] [NEWS-04] [NEWS-05] [NEWS-07]

The strongest legitimate consumer concern is not that E20 universally destroys engines. It is that owners of older, imported, carbureted or poorly documented vehicles often lack a convenient E10/E0 alternative and may bear mileage, verification, maintenance and warranty-evidence costs even when national benefits are real. “Compatible,” “certified” and “optimized” are not interchangeable. [GOV-01] [RES-02] [IND-01]

The strongest government case is energy security: displacing a portion of petrol with a domestically produced, high-octane oxygenate can reduce crude exposure and redirect expenditure toward Indian agriculture and industry, while many compliant vehicles can use E20 safely. [GOV-02] [GOV-08] [GOV-12]

The unresolved question is distributive: **if E20 benefits the country and is technically manageable for compliant vehicles, who should pay and provide evidence for the legacy fleet’s uncertainty—and should those owners retain a clearly labeled fuel choice?**

This is a strong Para Cast opportunity now. The winning angle is neither panic nor official reassurance alone, but a Malayalam evidence audit: what is physically expected, what manufacturers actually promise, what an owner can prove, and what policy still owes the consumer.

---

## 2. Current trend trigger

### Finding: a cascading trigger, with a late-July consumer-choice peak

The most precise answer is that India’s July 2026 E20 trend is **policy-driven at the base, media/platform-amplified in the middle, politically amplified after 14 July, and organically sustained by owner uncertainty**. No single event explains the whole curve.

| Date | Event | People/organizations | What changed in the conversation | Evidence |
|---|---|---|---|---|
| 17 Feb 2026; effective 1 Apr | Nationwide E20/default petrol specification with minimum RON 95 reported under the Essential Commodities framework | Union government, MoPNG, OMCs | Converted a phased programme into the fuel ordinary consumers encounter by default | [GOV-02] [GOV-03] |
| Mid-June | Posts alleging raw sugarcane “juice,” ants near fuel caps, rapid water absorption and old-vehicle destruction circulate | Social accounts, creators, owners | Technical concern becomes visual/meme-friendly misinformation | [GOV-04] [GOV-05] |
| 23 Jun | MoPNG/PIB issues misinformation clarification | MoPNG, PIB | First coordinated official response; also signals that the claims had reached material scale | [GOV-04] |
| 29–30 Jun; reported 1 Jul | Wording attributed to the Supreme Court that E20 was an “ongoing experiment” goes viral; Attorney General’s office says it was misreported | Supreme Court coverage, Attorney General’s office, news/social publishers | Authority conflict deepens distrust; a legal-sounding phrase becomes a reusable talking point | [NEWS-01] [GOV-05] |
| 2–4 Jul | Petroleum minister and an industry panel defend safety while acknowledging a modest mileage effect | Hardeep Singh Puri; Toyota, Maruti, Hero, TVS, Hyundai, Bajaj, EIL | Debate shifts from “does mileage fall?” to “how much, for which vehicle, and who pays?” | [NEWS-08] [GOV-06] |
| 5–7 Jul | Government backgrounder and myth/fact videos respond | PIB, NDTV and others | Official debunking expands, but consumer-choice and warranty questions remain | [GOV-05] [VID-05] |
| 12 Jul | Sourav Joshi vlog attributes Mercedes mileage falling from 17 to 9 to 5 km/l to E20; Mercedes issues compatibility advice | Sourav Joshi, Mercedes-Benz India | A huge creator transforms a technical topic into a personal “dashboard proof” story | [NEWS-02] [MFR-21] |
| 14 Jul | Creator later says an engine issue caused the mileage problem | Sourav Joshi, Moneycontrol | Corrects the central causal claim, but the original dramatic number travels farther than the correction | [NEWS-03] |
| 14 Jul | Arvind Kejriwal asks owners to submit complaint videos and seeks pure-petrol choice/price relief | Arvind Kejriwal, AAP | Crowd-sourced grievance collection and partisan amplification begin | [NEWS-04] |
| 14 Jul; published 16–17 Jul | Raipur consumer commission orders replacement/refund in a Grand Vitara dispute; Maruti alleges contamination and plans appeal | District consumer commission, owner, Maruti Suzuki | Gives consumers a legal template but leaves causation contested | [NEWS-10] [NEWS-11] |
| 20–23 Jul | Parliamentary answers repeat that no widespread substantiated failures are reported and acknowledge 3–5 percent mileage effects for some designs | Parliament, MoRTH/MoPNG | Official position becomes more precise but does not settle individual cases | [GOV-02] [NEWS-12] [NEWS-13] |
| 26–29 Jul | “E20 Janta Party” meme/campaign demands E0 choice, transparency and accountability; multiple similar accounts create attribution confusion | Campaign accounts, automotive media, political commenters | This is the immediate final-seven-day trigger: the issue becomes a consumer-rights identity rather than only a fuel question | [NEWS-05] [NEWS-06] [NEWS-07] |

### What people are actually interested in

The attention clusters around six questions:

1. **Choice:** Why can an owner of an older vehicle not buy clearly labeled E10 or E0?
2. **Mileage:** Is the loss 1–7 percent as engineering/official sources suggest for common categories, or the much larger loss reported by some owners?
3. **Compatibility:** What does the exact manual say, and why do brand statements use different dates?
4. **Warranty and proof:** Who pays if the OEM blames fuel and the OMC blames the vehicle?
5. **Price:** Why does a domestic blend not create a visible discount, especially if litres per kilometre rise?
6. **Fairness:** National import and farmer benefits versus concentrated costs for the legacy fleet.

The most shareable triggers were platform-native: ants, sugarcane imagery, a luxury-car dashboard, a party logo, and “government versus public” clips. The underlying concern is more serious than several memes: consumers do not have a simple, authoritative, model-specific decision tool or a routine evidence pathway when a fuel-related failure is alleged.

---

## 3. Google search and public-interest analysis

### What could be verified

Google Trends terms were prepared for “E20 petrol,” “E20 fuel,” “E20 petrol India,” “E20 mileage,” “E20 engine damage,” “E20 compatible vehicles,” “E20 petrol price,” “ethanol petrol India,” “E20 bike,” “E20 old car,” “E20 warranty,” “E20 Kerala,” and “E20 Malayalam.” The live Google Trends endpoint returned HTTP 429, and the signed-in browser bridge could not be initialized in this environment. Therefore this dossier **does not publish normalized index values, state rankings, percentage growth, or Google “breakout” labels**.

### Defensible relative pattern from independently dated public artifacts

- **Wave 1 — 23 June to 4 July:** the government clarification, the disputed court quote and the industry response indicate a rapid national discovery phase. [GOV-04] [NEWS-01] [GOV-06]
- **Wave 2 — 12 to 17 July:** the Sourav Joshi/Mercedes episode, the political grievance call and the consumer-commission order created the strongest owner-impact news cluster. [NEWS-02] [NEWS-03] [NEWS-04] [NEWS-10]
- **Wave 3 — 26 to 29 July:** “E20 Janta Party” shifted search/social language toward “pure petrol,” “choice,” “price,” “old vehicle” and “warranty.” [NEWS-05] [NEWS-06] [NEWS-07]

The visible related-query clusters in search results and current media were:

- E20 petrol price / why no price cut
- E20 mileage / mileage drop
- E20 compatible vehicles / car or bike model
- E20 old car / BS6 / BS3 / BS4
- E20 warranty / insurance
- pure petrol / E0 / E10 choice
- E20 Janta Party
- engine damage / corrosion / fuel pump / rubber hose
- E20 Kerala / Malayalam explainer

Malayalam evidence exists in current Onmanorama coverage, earlier high-reach Asianet material and engaged Kerala Reddit discussions. [NEWS-16] [VID-03] [SOC-01] [SOC-02] That proves regional relevance, not a Kerala-specific Google Trends rank.

### Competing search concepts

E20 now competes less with “ethanol blending target” and more with consumer-action searches: “is my car compatible,” “E0 petrol,” “fuel additive,” “warranty,” and brand/model terms. An editor should capture a fresh Google Trends export immediately before publication and compare:

- E20 petrol vs ethanol petrol India
- E20 mileage vs E20 engine damage
- E20 compatible vehicles vs E20 petrol price
- E20 petrol vs flex fuel
- E20 Kerala vs E20 Malayalam, using Kerala geography where the interface permits

### Direction and limitation

As of 29 July, attention is **elevated and still increasing or plateauing at a high level**, driven by the last-seven-day campaign and parliamentary coverage. That assessment is qualitative and event-based, not a Google normalized score. The lack of exact Trends data is a material limitation; it is preferable to an invented graph.

---

## 4. News and policy landscape

### Current policy position

- India’s policy goal moved from E10 to E20 through the National Policy on Biofuels, its 2022 amendment and the NITI roadmap. E10 was achieved in June 2022; E20 began at selected outlets in February 2023; vehicle material/compliance transitions followed; and ordinary petrol supply moved to a nationwide E20/RON 95 basis from April 2026. [GOV-01] [GOV-08] [GOV-09] [GOV-02]
- BIS IS 17021 is the central E20 fuel-quality reference. A blend that is contaminated, over-blended or otherwise outside specification is a different issue from compliant E20. [STD-01] [STD-02]
- As of the 23 July parliamentary answer, the government says no decision has been taken to mandate a blend beyond E20. Headlines that imply E30/E85/E100 is already decided are premature. [GOV-02] [GOV-07]
- MoPNG argues that E20 is safe for compatible vehicles, that catastrophic fleet-wide claims are unsupported, and that warranty/insurance is not automatically void. [GOV-04] [GOV-05] [IND-01]
- The same official record recognizes a mileage penalty for some vehicle categories. This is important: the defensible government case is not “zero downside,” but “manageable downside relative to national benefits.” [GOV-01] [GOV-02] [NEWS-08]

### Institutions and what each controls

| Institution | Role | Current editorial significance |
|---|---|---|
| MoPNG | Biofuel policy, OMC programme, procurement, blending and clarifications | Best source for programme status and official economic case; interested policy sponsor |
| MoRTH | Vehicle rules, homologation/emissions interface and parliamentary responses | Relevant to compatibility rules and fleet impact |
| NITI Aayog | Roadmap and cross-ministry transition design | Source of the best-known mileage and transition estimates [GOV-01] |
| BIS | Fuel specification | Defines compliant E20; use it to separate blend from contamination [STD-01] |
| OMCs | Procurement, blending, distribution and retail stations | Station-specific labeling, complaint handling and fuel sampling need direct OMC verification |
| Parliament | Current scrutiny and official answers | High-value primary record, but answers are the government’s evidence, not cross-examination [GOV-02] |
| SIAM/ARAI/FIPI | Industry and testing institutions | Strong compatibility/warranty position; not consumer regulator [IND-01] |
| Manufacturers | Model/manual/warranty authority | Each brand uses different dates and language; no universal year shortcut |
| Consumer commissions | Individual remedies | District orders are fact-specific and appealable, not national technical standards [NEWS-10] [NEWS-11] |

### What changed recently

The February/April 2026 nationwide specification reduced the practical availability of lower-ethanol alternatives. June and July clarifications did not change the blend target; they changed the government’s communication posture. The central policy gap remains a consumer-facing compatibility database, transparent retail labeling, an accessible alternative for genuinely incompatible vehicles, and a standardized fuel/part evidence protocol when a failure occurs.

### State-level position

No Kerala-specific technical exemption or state fuel standard was identified. Fuel taxation and pump prices vary by state, while blend specification is national. No authoritative Kerala-wide failure dataset was found. Regional stories should therefore focus on audience exposure and practical concerns, not claim a special Kerala chemical effect.

---

## 5. YouTube and video landscape

The companion CSV contains one row per selected video and the exact requested fields. Live YouTube pages could not be reliably opened, so only four search results exposed timestamped views and subscriber counts; other metrics are marked unavailable rather than inferred.

### Observed performance where public counts were visible

| Video/channel | Upload | Views | Subscribers | Views/subscriber |
|---|---:|---:|---:|---:|
| Mechanical Tech Hindi additive/E20 video | 18 Feb 2026 | 13,114 | 1.57m | 0.84% |
| MotorBeam rollout update | 12 Sep 2025 | 30,658 | 1.21m | 2.53% |
| Asianet Malayalam E20 explainer | 28 Aug 2025 | 133,167 | 11.5m | 1.16% |
| Kanak News April-mandate video | 28 Feb 2026 | 50,399 | 8.85m | 0.57% |

These are not a current “top videos” ranking; they are the only directly exposed view/subscriber pairs. The most consequential current video was Sourav Joshi’s 12 July vlog because of his reported 41.3m subscriber scale and the extreme 17→9→5 km/l claim, even though the relevant segment was reportedly altered/removed and later corrected. [NEWS-02] [NEWS-03]

### Repeated video formulas

1. **Personal dashboard as proof:** emotionally powerful, methodologically weak.
2. **Myths vs facts:** efficient, but often treats “not universal” as “not a consumer issue.”
3. **Deadline urgency:** captures clicks but compresses multiple transition dates.
4. **Mechanic/additive fix:** turns uncertainty into a product promise without independent evidence. [VID-01]
5. **Government/industry panel:** authoritative, but lacks adversarial consumer evidence. [VID-10]
6. **Malayalam safety question:** locally relevant, but often misses compatibility-versus-optimization and contract proof. [NEWS-16] [VID-03]

### What comments and titles imply

Recurring concerns are mileage, old bikes, warranty, additives, fuel-pump/seal failure, “why no choice?”, and “where is the price benefit?” These are representative themes across articles/forums, not a statistically sampled comment study.

### Para Cast response posture

The best approach is a multi-clip evidence audit, not a personal takedown. Use the Sourav claim and correction, an official industry clip, a Malayalam explainer, an additive claim, and an E20 Janta Party choice demand. For every clip, display its date and status (“original removed/edited,” “creator corrected,” “official claim,” or “uncontrolled demonstration”).

---

## 6. Social-media sentiment

### Participant map

| Group | Recurring position | Evidence status |
|---|---|---|
| Supporters | Energy security, farmer income, long international use, no fleet-wide catastrophe | Policy case supported in part; individual safety still model-specific |
| Critics | No E10/E0 choice, mileage/cost, rushed rollout, opaque conflicts and data | Choice and mileage are legitimate; conspiracy claims often unsupported |
| Vehicle owners | Dashboard mileage, rough running, fuel-pump/seal complaints, resale anxiety | Important leads; anecdotes cannot isolate fuel, maintenance or contamination |
| Mechanics | Hose, seal, carburetor and pump warnings; additive recommendations | Component knowledge can be useful; universal claims require test evidence |
| Engineers | Energy density, closed-loop fuel trims, materials, phase separation | Strongest explanatory layer when source/model assumptions are explicit |
| Environmental advocates | Import/climate benefit versus crop, land and water burden | Both sides are pathway-dependent [RES-04] [RES-06] [RES-07] |
| Farmers/producers | Stable demand, crop prices, rural investment | Real distributional benefit; crop and water effects vary |
| Political commentators | Government competence, alleged conflicts, price fairness | High amplification; accusations need documentary evidence |
| Automotive creators | High-share tests, warnings and quick fixes | Mixed quality; controlled methodology is rare |
| Confused consumers | “Which year?”, “Does premium mean E0?”, “Will insurance reject me?” | Reflects a real communication failure |

### Recurring complaints and fears

- Mileage has fallen more than government estimates.
- Older vehicles were designed before E20 and cannot opt out.
- Warranty claims will become a blame contest.
- Fuel pumps do not show enough blend/quality information.
- Domestic ethanol benefits producers while the driver pays the cost per kilometre.
- Water absorption, parked vehicles and corrosion are poorly explained.
- Premium petrol is assumed to be ethanol-free.

### Jokes, memes and political narratives

Ants near fuel caps, raw sugarcane/juice imagery, “E20 Janta Party,” minister resignation demands and “experiment on vehicles” phrases supply the meme language. [GOV-04] [NEWS-01] [NEWS-05] [NEWS-07] The first two are chemically misleading; the consumer-choice slogan points to a legitimate policy question.

### Kerala/Malayalam sentiment

Kerala threads show strong attention to two-wheelers, high fuel expenditure, old/used vehicles, storage while owners are away and the absence of choice. [SOC-01] [SOC-02] Coastal humidity alone is **not** evidence that E20 will fail in Kerala. It becomes relevant only through water ingress, unsealed storage, condensation cycles or poor station handling; no Kerala-wide comparative failure study was found.

---

## 7. Policy timeline

| Date | Development | Source |
|---|---|---|
| 2001 | Pilot use of ethanol-blended petrol begins in India | [GOV-01] |
| 2003–04 | Early programme/formal expansion attempts establish E5 direction | [GOV-01] |
| 2006 | E5 programme expands across multiple states/UTs, subject to availability | [GOV-01] |
| Jan 2013 | A renewed 5 percent blending framework is notified | [GOV-01] |
| 2018 | National Policy on Biofuels sets a long-range E20 target for 2030 and broadens feedstock policy | [GOV-09] [GOV-10] |
| Jun 2021 | NITI/MoPNG roadmap proposes E20 availability April 2023–April 2025, material-compatible/E10-tuned vehicles from April 2023 and E20-tuned vehicles from April 2025 | [GOV-01] |
| Jun 2022 | India reports achieving E10 ahead of schedule | [GOV-08] |
| Jun 2022 | Policy amendment advances E20 target from 2030 to ESY 2025–26 | [GOV-09] |
| 6 Feb 2023 | E20 launches at 84 outlets in 11 states/UTs; phased expansion begins | [GOV-03] |
| 1 Apr 2023 | Roadmap transition point for new E20 material-compatible vehicles | [GOV-01] |
| 2023–25 | Major car and two-wheeler brands announce portfolio/model transitions | [MFR-01] [MFR-11] [MFR-15] [MFR-16] |
| FY2024–25 / 2025 | National average blending approaches the 20 percent target; current-model E20 optimization/certification expands | [GOV-02] [GOV-07] |
| 17 Feb / 1 Apr 2026 | Nationwide E20 petrol with minimum RON 95 becomes the ordinary required supply, subject to notified exceptions | [GOV-02] [GOV-03] |
| 23 Jun–23 Jul 2026 | Government issues misinformation clarification, backgrounder, panel and parliamentary responses | [GOV-04] [GOV-05] [GOV-06] [GOV-02] |
| Current, 29 Jul 2026 | E20 remains the policy ceiling for which a decision has been announced; higher blends require new policy, compatible/flex-fuel vehicles and infrastructure | [GOV-02] |

Expected next developments: more OEM legacy-vehicle statements, retrofit offers for limited models, consumer litigation/appeals, better fuel-quality complaint procedures, and debate over choice/labeling. These are informed expectations, not announced deadlines.

---

## 8. Technical and engineering analysis

### 8.1 Composition and fuel properties

E20 is roughly 20 percent anhydrous ethanol and 80 percent petrol by volume, within a tolerance and quality envelope defined by BIS. Ethanol is polar and oxygen-containing; petrol is a non-polar hydrocarbon mixture. The blend is a standardized automotive fuel when it meets IS 17021—not raw sugarcane juice and not illicit adulteration. [STD-01]

Ethanol has high octane but lower lower-heating-value per litre than petrol. Octane measures resistance to knock, not stored energy. A high-compression/turbo engine with suitable ignition, compression and fueling can exploit anti-knock value; an unchanged engine cannot turn octane into free power. [GOV-01] [GOV-15]

### 8.2 Energy density and mileage

Pure ethanol contains roughly one-third less energy per gallon/litre than gasoline. On a simple volumetric-energy basis, E20 is about 6–7 percent below E0, while moving from E10 to E20 is roughly a further 3–4 percent depending on the base petrol and exact blend. Real mileage is not identical to energy ratio because engine efficiency, fuel trim, compression, ignition, load, traffic, tyre pressure, air-conditioning and measurement noise matter. [GOV-15]

The NITI roadmap estimated approximately:

- 6–7 percent lower fuel economy for four-wheelers designed for E0 and calibrated for E10;
- 3–4 percent for two-wheelers;
- 1–2 percent for four-wheelers designed for E10 and calibrated for E20. [GOV-01]

These are government/roadmap estimates, not a guarantee for every vehicle.

### 8.3 Air–fuel ratio and engine control

Gasoline’s stoichiometric air–fuel ratio is about 14.7:1 by mass; ethanol’s is about 9:1. E20 therefore needs more fuel mass/volume for the same air than pure petrol. A modern closed-loop injection system uses oxygen-sensor feedback and short/long-term fuel trims to compensate within injector and calibration limits. An older open-loop or carbureted engine has fixed jets/maps and may run leaner or show drivability/temperature changes if the calibration has insufficient margin.

Compatibility is thus two problems:

1. **Wetted materials:** tank, coatings, hoses, seals, O-rings, pump, injector and metal compatibility.
2. **Metering/calibration:** can the system deliver the correct fuel across start, idle, acceleration and full load?

### 8.4 Water, hygroscopicity and phase separation

Ethanol attracts and dissolves some water. That does not mean a sealed tank instantly pulls destructive quantities of water from humid air. If water exceeds the blend’s tolerance, an ethanol/water-rich phase can separate from the hydrocarbon-rich phase, creating poor combustion and corrosion risk. Temperature and fuel composition affect the threshold. Water can enter via station storage, transport, leaks, cap/seal faults, washing/flooding, condensation cycles or prolonged exchange with humid air. [RES-02] [MFR-14]

Editorial rule: **in-spec E20, over-blended fuel, and water-contaminated fuel are three different hypotheses.**

### 8.5 Materials and corrosion

Some older natural rubbers, elastomers, adhesives, plastics, zinc/brass/aluminum components or coatings can swell, harden, embrittle, leach or corrode in intermediate ethanol blends. Manufacturers progressively substituted compatible elastomers and treated metals. The change was not synchronized across every brand, model and part, so model year or BS stage cannot prove compatibility alone. [RES-02]

Compatible does not mean zero wear. It means the materials/system meet an acceptable design life under the approved fuel and conditions.

### 8.6 Pumps, injectors and carburetors

- **Fuel pumps:** ethanol-compatible windings, commutators, seals and housings matter; contamination/phase-separated water can cause distinct failures.
- **Injectors:** must have compatible materials and sufficient flow/fuel-trim range. Deposits may change after solvent exposure; an initial filter issue is possible in a dirty legacy system.
- **Carburetors:** fixed jets and bowls are more exposed to calibration and evaporation/storage issues. A proper retrofit may require hoses, seals, gaskets, float/needle parts and jetting—not merely an additive.
- **ECU/closed loop:** can correct cruising mixture but may have limited authority at cold start or high load; optimization adds maps and hardware assumptions.

### 8.7 Cold starting, storage and evaporative emissions

E20 has more cold-start volatility challenge than E0/E10 but far less than E85. Indian ambient conditions reduce, but do not eliminate, cold-start/calibration concerns. SAE/Indian testing reported acceptable startability/drivability for the tested vehicles. [RES-01] [GOV-01]

There is no defensible universal “safe for exactly X days” rule. A sound, compatible, sealed vehicle can tolerate ordinary storage; a legacy carbureted bike with a vented tank, old hoses and water contamination has a different risk. Follow the manual, keep the cap/seals sound, avoid unknown additives, and use fresh fuel/authorized storage procedure for long lay-up.

Ethanol can change evaporative emissions non-linearly because vapor pressure depends on the base gasoline and blend. The Indian roadmap reported broadly similar evaporative outcomes in its referenced tests, not a universal zero-change finding. [GOV-01]

### 8.8 Exhaust and lifecycle emissions

Oxygenated fuel can reduce carbon monoxide and unburned hydrocarbons, and sometimes particulate matter. The roadmap cites large CO reductions in tested categories and around 20 percent HC reduction, while NOx was unchanged or modestly higher depending on vehicle. Acetaldehyde/carbonyl emissions can increase. [GOV-01] [RES-03]

Tailpipe improvement is not lifecycle proof. Crop cultivation, fertilizer, irrigation, land-use change, distillery heat, residue collection and transport determine the full result. [RES-06] [RES-07]

### 8.9 Cars, motorcycles and scooters

Two-wheelers often have smaller engines, more carbureted/air-cooled legacy stock, smaller tanks, lower-cost components and high sensitivity to kilometre-per-litre changes. Modern fuel-injected BS6 Phase II motorcycles can be explicitly E20-designed. Passenger cars generally have more closed-loop authority and evaporative controls but also higher repair cost. Scooters share small-tank/storage concerns and model-specific hose/pump design. No category is uniformly safe or unsafe.

### Evidence hierarchy for any claimed failure

1. Fuel sample with chain of custody and accredited test: ethanol percentage, water and contamination.
2. Exact vehicle manual/fuel-cap/VIN and manufacturer bulletin.
3. Diagnostic trouble codes, fuel trims, compression, injector/pump pressure and failed-part examination.
4. Before/after data under comparable route, load, weather and driving.
5. Owner narrative and dashboard photograph.

The first four can support causation; the fifth generates a lead.

---

## 9. Vehicle compatibility

### The three-column rule

- **Material compatible:** fuel-wetted parts can tolerate E20.
- **Certified/homologated:** the vehicle/configuration was formally tested/declared for the fuel.
- **Optimized/calibrated:** design and ECU/fueling take advantage of the blend and reduce mileage/performance penalties.

A vehicle may be compatible but not optimized. [GOV-01] [MFR-20]

### Manufacturer evidence map

| Manufacturer | Best-supported current position | Evidence/confidence | What remains |
|---|---|---|---|
| Maruti Suzuki | Industry panel reports broad service experience without an E20 failure signal; current vehicles transitioned under industry schedule | [GOV-06], primary testimony, not model list | Exact pre-transition model/VIN guidance and contested Raipur case [NEWS-10] [NEWS-11] |
| Hyundai | Use model/year owner manual; warranty excludes improper fuel | [MFR-02] [MFR-03], high | No blanket historical cut-off should be inferred |
| Tata Motors | BS6 Phase II passenger range announced with E20-compatible engines in Feb 2023 | [MFR-01], high | Earlier models need manual/OEM answer |
| Mahindra | Reported company position: older petrol vehicles safe, newer E20-calibrated vehicles perform better | [MFR-20], moderate-high | Obtain original letter/vehicle-specific statement |
| Toyota | Post-1 Apr 2023 fully material compliant/tested; announcement discusses earlier vehicles’ capability | [MFR-04], high | Preserve exact announcement wording for pre-2023 models |
| Honda Cars | Current range compliant; Indian-made cars since 1 Jan 2009 described as materially compatible | [MFR-05], high | Imported/older variants and optimization date |
| Kia | Warranty terms and model manuals are authoritative | [MFR-09] [MFR-10], high | No reliable blanket brand/date statement captured |
| Renault | Current Duster/current product info shows E20 | [MFR-19], high for current product | Legacy Duster/Kwid/Triber variants need exact manual |
| Nissan | Magnite turbo since Aug 2024 and naturally aspirated since Feb 2025; specified warranty protection | [MFR-06], high | Older Nissan/Datsun/imported models |
| Volkswagen | Petrol cars after 1 Apr 2020 stated compatible | [MFR-07], high | Earlier vehicles are “not evaluated here,” not automatically incompatible |
| Škoda | BSVI petrol cars after 1 Apr 2020 stated compatible | [MFR-08], high | Earlier/imported models |
| MG | No authoritative brand-wide legacy cut-off found | [MFR-23], limited | Check current manual, fuel cap and written dealer/OEM response |
| Hero MotoCorp | Portfolio described as E20 compatible from Mar 2023; 2026 flex-fuel models accept E20–E85 | [MFR-11] [MFR-12], high | Older carbureted models; flex fuel is not ordinary E20 compatibility |
| Honda Motorcycle & Scooter India | Current OBD2B/BS6 products commonly advertise E20 compliance | [MFR-22], high for current pages | No blanket legacy statement captured |
| Bajaj | Individual manuals specify E20; broad “last 10 years” statement is reported | [MFR-17] [NEWS-17], mixed | Direct OEM document and model/KTM scope |
| TVS | Current/BS6-II E20 guidance; exact manuals approve up to E20 | [MFR-13] [MFR-14], high | Older models need manual/authorized retrofit advice |
| Royal Enfield | All models updated from 1 Apr 2023; select BS3/BS4 retrofit kits reported | [MFR-15] [NEWS-18], high current/moderate retrofit | Kit eligibility, parts, cost and warranty need dealer confirmation |
| Yamaha | Current R15 V4 advertises E20 compatibility | [MFR-18], high for current product | No brand-wide legacy statement captured |
| Suzuki Motorcycle | Domestic portfolio announced E20 compliant in June 2023 | [MFR-16], high current | Older models need manuals |
| KTM | No direct KTM India blanket source captured; some Bajaj-related reporting may include allied models | [NEWS-17], low-moderate | Treat as unresolved until manual/OEM confirmation |

### Model-year and BS-stage guidance

- **BS6 Phase II / OBD2B and newer:** strong probability of explicit E20 readiness, but verify the manual.
- **BS6 before brand transition:** uncertain; several brands give 2020, 2023, 2024 or 2025 dates.
- **BS4/BS3 and older:** do not assume either universal incompatibility or safety. Check part numbers, manuals and retrofit guidance.
- **Carbureted/vintage:** highest need for material and jetting review; consider a specialist and written fuel plan.
- **Imported vehicles:** Indian BS labels or local brand statements may not cover them; use the market-specific manual/VIN.
- **Flex-fuel vehicles:** specifically designed for a range such as E20–E85; they are not evidence that an ordinary E20-compatible vehicle may use E85.

### Owner verification workflow

1. Photograph fuel-cap label and VIN.
2. Download the exact manual edition.
3. Search for “ethanol,” “E20,” “gasohol,” “alcohol” and “fuel requirements.”
4. If unclear, email the manufacturer with VIN and ask: maximum permitted ethanol, whether materials are compatible, whether calibration is optimized, and whether warranty changes.
5. Save the response, receipts and service records.
6. Never treat a dealer’s oral assurance as equivalent to written OEM guidance.

---

## 10. Warranty and insurance questions

### Warranty

The categorical claim “E20 voids every warranty” is false. SIAM/ARAI/FIPI and several brands say that using in-spec E20 in approved vehicles does not automatically void warranty. [IND-01] [MFR-06] Warranty can still be disputed when:

- the exact model is not approved for that ethanol concentration;
- fuel is contaminated, adulterated or exceeds the specification;
- the owner used an unapproved additive/retrofit;
- maintenance or storage instructions were ignored;
- causation cannot be shown.

Hyundai and Kia warranty language illustrates a common issue: “improper/insufficient fuel” may be excluded, but compliant E20 is not automatically improper. [MFR-03] [MFR-09]

### Insurance

No standalone, current IRDAI circular specifically resolving all E20 mechanical-damage claims was located. Ordinary motor insurance covers defined perils, not routine wear or every mechanical breakdown. A collision/fire claim and a gradual seal/pump failure are different. The industry statement is reassuring but not a substitute for the policy wording. [IND-01]

### Contamination versus compatibility

The Raipur Grand Vitara dispute shows the evidentiary problem: the consumer commission ordered relief, while Maruti says the vehicle was E20 compatible and contaminated fuel caused the problem. [NEWS-10] [NEWS-11] The order is fact-specific and reportedly appealable. It should not be presented as a Supreme Court rule or scientific proof that E20 caused the failure.

### Consumer remedy/evidence pathway

1. Stop driving if there is a safety risk; do not add another chemical.
2. Obtain an authorized-service diagnostic printout and preserve failed parts.
3. Keep the fuel receipt, station/time/pump number and previous fill history.
4. Ask the OMC for the formal fuel-quality complaint and sampling process.
5. Request a sealed split sample with chain of custody and accredited testing where possible.
6. Seek a written manufacturer decision citing the exact manual/warranty clause.
7. Escalate through the OMC, OEM, National Consumer Helpline and consumer commission if unresolved.

### Labeling gap

BIS defines the fuel, but this pass did not locate a single, consolidated current rule showing exactly what every pump must display about ethanol percentage and alternative-grade availability. The editor should film current station labels and obtain the OMC circular before making a legal “must label” claim. The consumer-rights case is strongest as a transparency recommendation, not an unsupported assertion of current non-compliance.

---

## 11. Mileage and consumer cost

### Evidence bands

- **Laboratory/energy expectation:** E20 has lower volumetric energy than E0; E10→E20 usually implies a few percent extra volume if efficiency is unchanged. [GOV-15]
- **Government/NITI estimate:** 1–2 percent for E20-designed/E20-calibrated four-wheelers; 3–4 percent for two-wheelers; 6–7 percent for older E0-designed/E10-calibrated four-wheelers. [GOV-01]
- **Manufacturer claim:** many current vehicles are calibrated/compatible; few publish a model-specific guaranteed fuel-economy delta.
- **Independent Indian testing:** SAE work supports acceptable drivability/material performance in tested vehicles, but not every model/year. [RES-01]
- **Owner survey:** LocalCircles reports much larger self-reported losses, valuable as dissatisfaction evidence but not controlled causation. [NEWS-09]
- **Viral anecdote:** the 17→5 km/l Mercedes story is physically too large to attribute to blend energy alone and was later linked by the creator to an engine issue. [NEWS-02] [NEWS-03]

### Variables that can overwhelm the blend signal

Route, traffic, temperature, air-conditioning, tyre pressure, load, driving style, trip length, idle time, dashboard algorithm, refill error, seasonal base petrol, service condition, oxygen sensor, injector/pump pressure and fault codes.

### Illustrative car scenario — not measured

Assumptions: 15 km/l baseline, 1,000 km/month, ₹100/litre, price unchanged.

| Mileage loss | New mileage | Extra litres/month | Extra litres/year | Extra cost/year |
|---:|---:|---:|---:|---:|
| 3% | 14.55 km/l | 2.06 | 24.7 | ₹2,474 |
| 5% | 14.25 km/l | 3.51 | 42.1 | ₹4,211 |
| 7% | 13.95 km/l | 5.02 | 60.2 | ₹6,022 |

Method: baseline monthly litres = 1,000/15 = 66.67; scenario litres = distance/new mileage; difference × 12 × ₹100.

### Illustrative two-wheeler scenario — not measured

Assumptions: 50 km/l baseline, 800 km/month, ₹100/litre.

| Mileage loss | New mileage | Extra litres/month | Extra litres/year | Extra cost/year |
|---:|---:|---:|---:|---:|
| 3% | 48.5 km/l | 0.495 | 5.94 | ₹594 |
| 4% | 48.0 km/l | 0.667 | 8.00 | ₹800 |

The rupee total looks smaller than for a car, but the household impact can still be important because motorcycles are high-frequency mobility tools and owners track km/l closely.

### How Para Cast should describe mileage

“A few-percent physical penalty is expected in many non-optimized vehicles; a particular owner can see less or more, but a huge fall requires diagnosis. Compare E10 with E20—not E0 with E20—and do a repeatable brim-to-brim test.”

---

## 12. Petrol price and ethanol economics

### Why domestic does not automatically mean cheaper at the pump

Retail petrol price is not a transparent pass-through of the cheapest blend component. It includes:

- refinery/OMC product economics;
- ethanol procurement by feedstock category;
- blending, storage and distribution;
- central duties and state VAT;
- dealer margin;
- freight and policy pricing decisions.

Government Q&A cited current ethanol procurement around ₹71.86/litre before GST, logistics and storage and argued that E20 becomes clearly cheaper than petrol only at much higher crude prices in its comparison. [GOV-03] [GOV-12] The exact break-even changes with crude, exchange rate, refinery yield, feedstock and tax assumptions. [NEWS-14] [NEWS-15]

### Four different “savings”

1. **Gross crude displacement:** fewer litres of fossil petrol in the blend.
2. **Foreign-exchange saving:** official estimate of avoided import expenditure.
3. **OMC blend economics:** procurement plus blending/logistics relative to petrol.
4. **Consumer cost per kilometre:** pump price divided by real mileage.

These can move in different directions. A country can save foreign exchange while an owner pays the same price per litre and slightly more per kilometre.

### Who benefits

Farmers/feedstock suppliers, sugar mills, grain processors, distilleries, logistics and equipment providers gain a domestic demand stream. Government cites large cumulative farmer payments and gross forex savings. [GOV-08] [GOV-12] These are policy benefits, but the word “saving” should be labeled as an official gross estimate, not a household discount or independently audited net welfare calculation.

### Incentives and costs

The programme uses administered differential ethanol prices, concessional finance/interest-subvention mechanisms, lower GST treatment for EBP ethanol, long-term procurement and infrastructure expansion. Costs include distillery capex, feedstock/seasonality, water/effluent management, separate storage and quality control.

### Direct answer

E20 is not necessarily cheaper to the consumer because ethanol is not uniformly cheaper after procurement/logistics, retail prices are tax- and policy-heavy, and E20 contains less energy per litre. The relevant fairness question is not only ₹/litre; it is ₹/km plus who receives the import/agricultural benefit.

---

## 13. Agriculture, water and food security

### Feedstocks are plural

Indian ethanol may come from sugarcane juice/syrup, B-heavy or C molasses, maize, damaged food grains, surplus FCI rice and second-generation agricultural residue. [GOV-09] [GOV-10] “E20 equals sugarcane ethanol” is false.

### Farmer-income case

A reliable ethanol offtake can stabilize demand, pay mills/distilleries, support crop prices, improve sugar-mill liquidity and diversify rural industry. Government farmer-payment and import-substitution claims are a genuine distributional argument. [GOV-08] [GOV-12]

### Water: boundary discipline

Two commonly mixed numbers can both be true:

- **Distillery process water:** modern plants may report a few litres of process water per litre of ethanol, with recycling/zero-liquid-discharge systems.
- **Agricultural water footprint:** includes rainfall and/or irrigation required to grow the crop and can reach thousands of litres per litre of sugarcane- or rice-derived ethanol.

The NITI roadmap cites approximately 1,600–2,100 litres of water per kilogram of sugar and around 3,000 litres for the sugar needed for one litre of ethanol under its stated boundary. [GOV-01] No single national “water per litre” number is valid without crop, district, irrigation source and accounting boundary.

### Crop trade-offs

- **Sugarcane:** established supply chain and high yield; serious irrigation pressure in water-stressed regions.
- **Maize:** potentially lower water in suitable rain-fed areas, but competes with feed, starch and food markets; rapid demand can alter prices and land.
- **Rice:** surplus/damaged stock can reduce waste, but rice is often water- and procurement-intensive; “surplus” is policy- and year-specific.
- **Molasses:** co-product logic improves allocation, but additional demand can induce more cane/sugar-sector expansion.
- **Residues/2G:** avoids direct food diversion and can reduce burning, but collection, competing soil/fodder uses, pre-treatment, enzyme cost and plant capex constrain scale.

CSTEP scenario work suggests that maintaining/expanding crop-based pathways may require substantial additional maize land under certain 2030 assumptions. [RES-04] The Economic Survey and peer-reviewed work emphasize feedstock and regional trade-offs. [GOV-11] [RES-06]

### Food-versus-fuel conclusion

The correct question is not “food or fuel?” in the abstract. It is: which feedstock, grown where, using what water, displaced from what market, under what price support, and with what residue/land-use counterfactual?

---

## 14. Environmental analysis

### Tailpipe

Expected/observed direction in tested vehicles:

- carbon monoxide: generally lower;
- unburned hydrocarbons: generally lower;
- particulate matter: can be lower;
- NOx: unchanged or sometimes modestly higher depending on calibration/temperature;
- acetaldehyde and some carbonyls: can increase;
- evaporative emissions: formulation-dependent, not automatically lower. [GOV-01] [RES-03]

### Well-to-wheel

Replacing fossil petrol can reduce fossil energy and crude transport/refining burden. The benefit depends on the ethanol pathway’s agricultural inputs, electricity/steam source, co-product allocation and logistics.

### Full lifecycle

Feedstock cultivation can add fertilizer nitrous oxide, irrigation energy, pesticide/toxicity burden, water depletion and land-use change. Molasses-based Indian modeling finds possible climate/fossil/PM benefits alongside increased land, water and toxicity impacts. [RES-07] Broader scenarios show that maize/rice/cane choices can change the sign and size of benefits. [RES-06]

### What “cleaner” should mean on screen

Never display one green arrow. Use a three-layer card:

1. **Tailpipe:** some pollutants down; carbonyl caveat.
2. **Fossil/import:** petrol displacement up.
3. **Farm/lifecycle:** feedstock-dependent water, fertilizer, land and distillery energy.

E20 can be climate-better than fossil petrol in a well-managed pathway without being environmentally harmless.

---

## 15. Government’s strongest case

1. **Energy security:** India imports much of its crude; replacing a share of petrol with domestic ethanol reduces exposure to global oil and currency shocks. [GOV-08] [GOV-12]
2. **Strategic resilience:** a diversified domestic liquid-fuel supply supports mobility during geopolitical or supply disruptions.
3. **Rural value:** procurement transfers part of transport expenditure to farmers, mills, grain processors, distilleries and rural industry.
4. **Octane:** ethanol is a high-octane oxygenate and can help meet RON requirements while enabling suitable engine design.
5. **Selected emissions:** controlled tests show lower CO/HC and potential PM reductions. [GOV-01]
6. **Fleet evidence:** India and other countries have substantial experience with intermediate blends, and no verified fleet-wide engine-destruction pattern has emerged. [GOV-02] [GOV-06]
7. **Managed transition:** the roadmap announced material and calibration transition dates years in advance. [GOV-01]
8. **Industrial development:** distilleries, enzymes, 2G technologies, storage and quality infrastructure create domestic capabilities.
9. **Current proportionality:** the government acknowledges modest mileage differences rather than claiming a free lunch. [NEWS-08]

The strongest version is: “E20 is a standardized national energy-security measure whose aggregate benefits are meaningful and whose technical risks are manageable with compliant fuel, compatible vehicles, good storage and honest manufacturer guidance.”

---

## 16. Critics’ strongest case

1. **The legacy fleet cannot opt out:** a policy can be nationally rational and still be unfair to owners who purchased under E5/E10 assumptions.
2. **Compatibility is not optimization:** “safe to use” can still mean more litres, worse drivability or shorter component life.
3. **Consumer cost is obscured:** no clear pump discount offsets lower energy for many users.
4. **Evidence burden is asymmetric:** a consumer rarely has a sealed fuel sample or independent failed-part analysis; OEM and OMC can blame each other.
5. **Warranty clarity is fragmented:** press statements do not replace contract-level, VIN-specific assurance.
6. **Labeling and choice are weak:** consumers want visible blend percentage, E0/E10 availability and a model lookup.
7. **Implementation pace:** fleet transition schedules and fuel transition did not eliminate millions of older vehicles.
8. **Environmental claims can be selective:** tailpipe CO is not the same as lifecycle climate/water performance.
9. **Food and water opportunity cost:** crop incentives can alter land, feed prices and groundwater use. [GOV-11] [RES-04] [RES-06]
10. **Benefits and costs fall on different people:** gross forex/farmer benefits are diffuse; repair and mileage costs are concentrated.

The strongest version is: “Even if compliant E20 is not an engine poison, a mandatory/default rollout without convenient alternatives, model-specific guarantees and an evidence pathway shifts avoidable uncertainty onto consumers.”

---

## 17. Fact-check matrix

The complete 25-row database is supplied in CSV and XLSX. High-priority verdicts:

| Claim | Verdict | Short reason | Sources |
|---|---|---|---|
| E20 destroys every old engine | False | Risk is model/material/condition-specific, not universal | [GOV-01] [RES-02] |
| E20 cuts mileage exactly 20% | False | Energy and official estimates imply smaller category-dependent effects | [GOV-01] [GOV-15] |
| E20 causes immediate corrosion | False | Incompatibility/water/storage can create risk; immediacy/universality is wrong | [RES-02] [STD-01] |
| Every post-year-X vehicle is compatible | Misleading | OEM transitions differ | [MFR-05] [MFR-06] [MFR-07] |
| E20 is cheaper | Misleading | Pump and cost/km depend on procurement, tax and energy | [GOV-03] [NEWS-14] |
| E20 is environmentally harmless | False | Tailpipe and lifecycle outcomes differ | [RES-03] [RES-06] [RES-07] |
| E20 voids every warranty | False | In-spec approved fuel is not an automatic void; exclusions still matter | [IND-01] [MFR-03] |
| Premium petrol is ethanol-free | False | Octane branding is not an E0 specification | [STD-01] |
| Additive makes every old vehicle safe | Unsupported | Cannot replace incompatible hardware/calibration | [RES-02] [VID-01] |
| 17→5 km/l is E20 energy alone | False | Magnitude is implausible; creator later cited engine fault | [NEWS-02] [NEWS-03] |
| All BS6 vehicles are compatible | Misleading | Emissions standard is not a universal fuel certificate | [MFR-01] [MFR-07] |
| Compatible equals optimized | False | Materials, homologation and calibration are distinct | [GOV-01] |

Editorial rule: a fact-check verdict answers the claim as worded. “False” to a universal claim does not prove zero individual risk.

---

## 18. Global comparison

| Market | Typical approach | Choice/labeling | Vehicle context | Lesson for India |
|---|---|---|---|---|
| Brazil | Standard gasoline moved to E30 in Aug 2025 | Flex-fuel drivers can also use hydrous ethanol where offered | Decades-old flex-fuel ecosystem and sugarcane industry | High blends work best when fleet design and consumer choice mature together [GOV-13] |
| United States | E10 common; E15 for eligible 2001+ light-duty vehicles; E85 for flex-fuel | Strong pump labeling and exclusions; grade availability varies | Motorcycles/non-road excluded from E15 eligibility | Misfueling controls and eligibility labels matter [GOV-14] |
| European Union | Common E5/E10/E85 label symbols; national grade mix varies | Multiple grades often coexist | Broad fleet and member-state variation | Standard labels reduce ambiguity; availability is separate [GOV-19] |
| Thailand | Gasohol grades including E20 coexist with gasoline products | Price/grade choice is visible, though policy evolves | Long gasohol experience | Differentiated prices and products can make consumer trade-offs legible [GOV-16] |
| China | E10 expansion was regionally uneven; nationwide ambitions were not fully universalized | Regional implementation | Food/feed and supply constraints influenced policy | Mandates must follow reliable feedstock and local implementation capacity |
| Australia | E10 labeling/quality regulation with other petrol grades available | Clear grade labels and parallel fuels | Vehicle compatibility guidance widely used | Consumer information and alternatives reduce legacy-fleet conflict [GOV-17] |
| Philippines | E10 mandate/framework; higher blends periodically considered | Current product/label rules require local verification | Regional biofuel/feedstock considerations | Pilot and certify before higher mandatory blends [GOV-18] |
| India | E20 default/nationwide from Apr 2026; flex-fuel products emerging | E10/E0 not routinely available nationwide | Large mixed-age two-/four-wheeler fleet | Build a VIN/model database, test protocol, labeling and limited legacy alternative |

Brazil and India are not interchangeable. Brazil combines decades of consumer familiarity, flex-fuel sensing/calibration, hydrous-ethanol distribution and a mature cane industry. India’s rapid E10→E20 transition meets a much larger two-wheeler/legacy-fleet communication problem.

---

## 19. Kerala and Malayalam-audience angle

### Verified relevance

- Malayalam newsrooms have published E20 explainers, and Kerala online communities show engaged discussion. [NEWS-16] [VID-03] [SOC-01]
- Mileage and fuel price are natural audience hooks because the issue concerns recurring household mobility cost.
- Two-wheelers deserve their own segment; NITI’s expected penalty and older carbureted components differ from optimized cars. [GOV-01]
- Used vehicles and stored vehicles make documentation and fuel-history questions especially practical.

### Plausible but not proven regional factors

- **Coastal humidity:** relevant only through actual water ingress/air exchange/storage. No evidence shows that Kerala humidity automatically makes compliant E20 unsafe.
- **Long storage while owners work abroad:** risk depends on tank sealing, duration, materials and maintenance; there is no universal E20 countdown.
- **Imported/Gulf-return vehicles:** market-specific manuals may not approve Indian E20; verify VIN/manual.
- **Limited local production benefit:** Kerala may receive fewer direct crop/distillery gains than producing states, but a quantified net fiscal/economic comparison was not found.
- **Fuel prices:** use a same-day official/OMC price screenshot before publication; prices were not frozen into this dossier.
- **Kerala search rank:** not claimed because live Trends geography was unavailable.

### Best Malayalam framing

“E20 കേരളത്തിൽ വേറൊരു കെമിസ്ട്രിയല്ല; പക്ഷേ കേരളത്തിലെ പഴയ ബൈക്ക്, ഉപയോഗിച്ച കാർ, നീണ്ട പാർക്കിംഗ്, മൈലേജ്-സെൻസിറ്റീവ് കുടുംബം—ഇവയ്ക്ക് രേഖയും തിരഞ്ഞെടുപ്പും കൂടുതൽ പ്രധാനമാണ്.”

This is a research framing line, not finished narration.

### Practical Kerala checklist

1. Check the exact manual/fuel cap, not WhatsApp model-year charts.
2. For a stored bike/car, follow the manual and keep the tank/cap system sound.
3. Keep receipts and record the station/pump.
4. If symptoms start immediately after filling, preserve evidence before draining/mixing.
5. Do not add a “universal E20 solution” without OEM approval.
6. For imported vehicles, obtain written fuel approval.

---

## 20. Content gaps

Scores are 1–5; total maximum 30.

| Rank | Underdeveloped angle | Orig. | Relevance | Evidence | Visual | Comments | Para Cast | Total |
|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | Compatibility vs certification vs optimization | 5 | 5 | 5 | 5 | 5 | 5 | 30 |
| 2 | National benefit vs individual cost: who pays? | 5 | 5 | 5 | 4 | 5 | 5 | 29 |
| 3 | A failure evidence chain: E20 vs excessive blend vs water contamination | 5 | 5 | 4 | 5 | 5 | 5 | 29 |
| 4 | Why octane is not energy and does not guarantee power/mileage | 4 | 5 | 5 | 5 | 4 | 5 | 28 |
| 5 | What warranty language actually does—and does not—promise | 5 | 5 | 4 | 4 | 5 | 5 | 28 |
| 6 | E10→E20 vs E0→E20: the denominator mistake | 5 | 5 | 5 | 5 | 3 | 5 | 28 |
| 7 | Two-wheelers/carburetors need a different analysis from cars | 4 | 5 | 5 | 5 | 4 | 5 | 28 |
| 8 | Pump labeling, E0/E10 choice and consumer rights | 4 | 5 | 4 | 4 | 5 | 5 | 27 |
| 9 | Parked vehicles: sealed tank, water ingress and no universal timer | 5 | 5 | 4 | 5 | 4 | 4 | 27 |
| 10 | ₹/litre versus ₹/km versus national forex saving | 5 | 5 | 5 | 4 | 4 | 4 | 27 |
| 11 | Kerala humidity: plausible mechanism versus regional myth | 5 | 4 | 3 | 5 | 4 | 5 | 26 |
| 12 | Tailpipe pollution versus lifecycle crop/water footprint | 4 | 4 | 5 | 5 | 3 | 4 | 25 |
| 13 | How to conduct a valid owner mileage test | 4 | 5 | 4 | 5 | 4 | 3 | 25 |
| 14 | What Brazil comparisons omit about flex-fuel choice | 4 | 4 | 5 | 4 | 3 | 4 | 24 |
| 15 | Additive claims: what a bottle cannot change | 4 | 5 | 4 | 5 | 4 | 5 | 27 |

Most existing content either reassures or alarms. The gap is **procedural evidence**: how an owner finds the applicable promise, measures mileage, preserves a fuel sample, and separates fuel specification from vehicle condition.

---

## 21. Reaction-video opportunity

### Recommended format

A **Malayalam multi-clip evidence audit** combining myth-versus-fact, government-claim-versus-consumer-experience and a practical owner checklist. Avoid a one-person takedown.

### Up to five reaction targets

| Target | Original URL | Claim | Correct part | Misleading/missing | Para Cast addition | Usage |
|---|---|---|---|---|---|---|
| Sourav Joshi Mercedes segment | Original stable URL not captured; report at [NEWS-02] | E20 caused 17→9→5 km/l | A dashboard anomaly deserved diagnosis | Energy difference cannot explain magnitude; creator later cited engine issue [NEWS-03] | Causation checklist and correction asymmetry | **Essential claim, optional clip.** Use a brief rights-cleared excerpt or newsroom screenshot; show correction prominently |
| E20 Janta Party campaign | [NEWS-06], handle must be verified because of duplicate accounts [NEWS-07] | Consumers need E0 choice/transparency | Choice and legacy uncertainty are legitimate | Follower totals/attribution volatile; some slogans overstate damage | Turn protest into specific policy questions | **Essential theme, optional post.** Recreate slogans as text; do not harass users |
| Mechanical Tech Hindi additive video | [VID-01] | Conditioner improves mileage up to 20% / solves E20 issues | Maintenance can affect mileage | No universal additive can change materials or certification | Explain evidence standard for additives | **Useful challenge.** Short quotation/screenshot, no endorsement |
| Onmanorama explainer | [NEWS-16] | Is E20 reducing mileage/damaging vehicles? | Correct Malayalam relevance and core questions | Limited room for contract, contamination and optimization distinctions | Add Kerala storage/import/used-bike workflow | **Constructive comparator.** Reference rather than attack |
| ET/government-industry response | [VID-10] | Catastrophic claims are false and fleet experience is reassuring | Good rebuttal to universal myths | Industry testimony is not independent; choice/cost remains | Steelman both sides and ask who bears uncertainty | **Essential official-side clip.** Use a short excerpt with source/date |

Copyright-safe principle: quote the minimum necessary for analysis, transform with commentary, retain attribution/date, avoid downloaded full clips, and obtain permission where platform/reuse rules require it.

---

## 22. Audience questions

### Vehicle compatibility

1. How do I check whether my exact car or bike—not just its brand—is E20 compatible?
2. Is the fuel-cap sticker or the owner manual more authoritative?
3. Does “BS6” automatically mean E20 compatible?
4. What is the difference between E20 compatible, certified and optimized?

### Mileage

5. How much mileage loss should I physically expect from E10 to E20?
6. Why do some owners report more than the government estimate?
7. Can higher octane recover the energy-density loss?
8. How do I run a fair brim-to-brim mileage comparison?

### Engine damage

9. Which hoses, seals, metals or plastics are most vulnerable?
10. Can E20 damage a fuel pump or injector?
11. How can a mechanic distinguish E20 incompatibility from contaminated fuel?
12. Does ethanol clean deposits and block a filter in an old vehicle?

### Warranty

13. Can an OEM reject a claim because the manual mentions only E10?
14. What written proof should I obtain from the manufacturer?
15. Does using a fuel additive affect warranty?
16. Who pays for testing if the OMC and OEM blame each other?

### Fuel price

17. Why is E20 not cheaper if ethanol is made in India?
18. Should fuel be compared by price per litre or price per kilometre?
19. Are premium petrol grades ethanol-free?
20. Why can’t an incompatible-vehicle owner buy E10 or E0?

### Environment

21. Which tailpipe pollutants fall with E20?
22. Why can acetaldehyde increase?
23. Is E20 better for climate after farming and distillation?
24. Does ethanol reduce particulate pollution?

### Agriculture

25. How much Indian ethanol comes from sugarcane, maize, rice and residues?
26. Does ethanol improve farmer income?
27. Does maize ethanol raise animal-feed or food prices?
28. What is the correct water footprint per litre?

### Government policy

29. When did E20 become the ordinary nationwide fuel?
30. Has India decided to mandate E30, E85 or E100?
31. Which ministry is responsible for vehicle compatibility and which for fuel quality?
32. What pump labels and fuel tests can a consumer demand?

### Two-wheelers

33. Are carbureted motorcycles at higher risk than fuel-injected ones?
34. Do scooters have different storage issues?
35. Are BS4 bikes eligible for manufacturer retrofit kits?
36. Why does a 3–4 percent loss matter to a daily commuter?

### Older cars

37. Can an old car be retrofitted with ethanol-compatible hoses and seals?
38. Is model year alone enough to select parts?
39. What should a vintage-car owner do if E0 is unavailable?
40. Will occasional use of E20 be different from continuous use?

### Kerala-specific concerns

41. Does Kerala’s humidity automatically cause phase separation?
42. What should an NRI owner do before leaving a vehicle parked for months?
43. Are Gulf-imported vehicles approved for Indian E20?
44. Where can a Kerala owner get an accredited fuel test?

### Practical consumer actions

45. What evidence should I collect immediately after a suspicious fill?
46. Should I drain the tank or first preserve a sample?
47. Can I mix E20 with a lower-ethanol grade if I find one?
48. What warning symptoms require stopping the vehicle?

### Top 10 questions

1. Is my exact model/VIN approved for E20?
2. Compatible, certified or optimized—which promise do I actually have?
3. Why is there no reliable E10/E0 choice for legacy vehicles?
4. What mileage loss is expected from E10 to E20?
5. If a failure occurs, how do I prove fuel quality and causation?
6. Can warranty be denied and what written protection should I obtain?
7. Why does E20 not lower my price per litre or kilometre?
8. Does Kerala humidity/storage make the risk different?
9. What do lifecycle water and climate numbers really measure?
10. What can a universal “E20 additive” not fix?

---

## 23. Visual research plan

| Visual | Purpose | Verifiable source | Rights/licensing | Recreate? |
|---|---|---|---|---|
| 2001–2026 policy timeline | Show acceleration and transition dates | [GOV-01] [GOV-09] [GOV-02] | Government-document excerpts with attribution; avoid full-page reproduction | Yes, original timeline |
| E20 molecule/blend card | Explain 20% volume and oxygenated fuel | [STD-01] | Original diagram | Yes |
| Octane vs energy gauges | Destroy the “higher octane = more energy” misconception | [GOV-15] | Use sourced numbers, original artwork | Yes |
| E0/E10/E20 energy bars | Show correct denominators | [GOV-15] | Original chart | Yes |
| Closed-loop vs carburetor diagram | Explain fuel-trim difference | [RES-01] [RES-02] | Original schematic, no proprietary service drawing | Yes |
| Hose/seal/pump materials cutaway | Explain compatibility | [RES-02] | Original generic parts; do not imply a brand part failed | Yes |
| Water/phase-separation animation | Separate hygroscopicity from inevitable failure | [RES-02] [MFR-14] | Original animation | Yes |
| Manufacturer evidence table | Make dates and uncertainty visible | [MFR-01]–[MFR-23] | Quote short exact lines with links; manufacturer marks nominative only | Yes, with source footer |
| Mileage scenario calculator | Translate percent into litres/₹ | Section 11 assumptions | Original; clearly label hypothetical | Yes |
| ₹/litre → ₹/km flow | Explain why domestic ethanol need not mean consumer discount | [GOV-03] [NEWS-14] | Original | Yes |
| Tailpipe vs lifecycle split-screen | Avoid “clean/dirty” binary | [GOV-01] [RES-03] [RES-07] | Original sourced icons | Yes |
| Feedstock/water boundary chart | Distillery process water vs agricultural footprint | [GOV-01] [RES-06] | Original; name boundary on every bar | Yes |
| Viral claim/correction timeline | Show Sourav allegation and backtrack | [NEWS-02] [NEWS-03] | Fair-use short screenshots with commentary; avoid full video | Prefer recreated headline cards |
| E20 Janta Party/choice card | Show late-July trigger | [NEWS-05] [NEWS-06] [NEWS-07] | Verify handle; minimize user content; blur personal data | Recreate slogans with attribution |
| Google Trends chart | Quantify interest before publication | Google Trends export, not captured here | Export/screenshot under platform terms | Yes after editor captures data |
| Kerala owner checklist | Provide immediate value | Sections 9–10 and manuals | Original | Yes |
| Fuel pump/label footage | Test transparency in the real world | Same-day local station filming + OMC response | Obtain station permission where required; avoid staff faces/plates | Original footage |

Do not use stock footage of rusted engines as evidence of E20 damage. Label every screenshot “claim,” “official statement,” “manual,” or “measured test.”

---

## 24. Para Cast content score

Scores are 1–10. Weights sum to 100.

| Criterion | Score | Weight | Weighted points |
|---|---:|---:|---:|
| Current search interest | 9 | 12 | 10.8 |
| Malayalam relevance | 9 | 10 | 9.0 |
| Consumer impact | 10 | 12 | 12.0 |
| Controversy | 10 | 8 | 8.0 |
| Educational value | 10 | 10 | 10.0 |
| Emotional relevance | 9 | 7 | 6.3 |
| Visual potential | 9 | 7 | 6.3 |
| Reaction-video potential | 9 | 7 | 6.3 |
| Comment potential | 10 | 7 | 7.0 |
| Evergreen value | 8 | 6 | 4.8 |
| Evidence availability | 8 | 8 | 6.4 |
| Risk of misinformation | 9 | 6 | 5.4 |

**Raw total:** 110/120.  
**Weighted opportunity score:** 92.3/100, or **9.2/10**.

“Risk of misinformation” is scored as audience/editorial salience; it is also a production risk. The package has high primary-source availability, but model-specific compatibility, exact pump labeling, live search data and social metrics require disciplined caveats.

**Publishing window:** now—ideally within 72 hours, while the late-July consumer-choice wave and parliamentary clarification overlap. If delayed, refresh policy statements, Google Trends, pump prices, video counts and the Raipur appeal status.

**Proceed now:** Yes, with a multi-clip Malayalam evidence audit and a strong practical checklist.

---

## 25. Final editorial recommendation

1. **Why cover now:** a technically important national policy has become a high-attention consumer-rights issue, with viral misinformation and legitimate unresolved costs.
2. **Strongest central question:** If E20 benefits India and is safe in compatible vehicles, who bears the cost and uncertainty for legacy owners who cannot choose E10/E0?
3. **Most defensible thesis:** E20 is neither a universal engine poison nor a free green win; the fuel can deliver national benefits, but the rollout needs model-specific guarantees, transparent labeling/choice and a fair evidence/remedy system.
4. **Most surprising verified fact:** “Compatible” can mean the fuel system tolerates E20 while the vehicle is not E20-optimized—so safety and mileage are separate claims. [GOV-01]
5. **Most common misconception:** higher octane means more energy, power or mileage.
6. **Strongest government argument:** domestic high-octane blending reduces crude exposure and channels spending into Indian agriculture/industry without evidence of fleet-wide catastrophic damage.
7. **Strongest consumer argument:** mandatory/default fuel transfers mileage, documentation and failure-proof costs to owners who bought vehicles under earlier fuel assumptions.
8. **Best reaction target:** the 17→5 km/l Sourav Joshi allegation plus its engine-fault correction, paired with the official industry response—not the creator alone.
9. **Ideal format:** 18–22 minute Malayalam multi-clip evidence audit with three acts: why it trends; engineering/compatibility; rights and practical action.
10. **Recommended duration:** 20 minutes, plus a 60–90 second checklist short.
11. **Primary Malayalam audience:** daily two-wheeler commuters, owners of 2005–2023 petrol vehicles, used-car buyers, families storing vehicles and imported-vehicle owners.
12. **One sentence viewers should remember:** **E20 risk is not decided by one viral mileage number or one model year—check the exact vehicle, the exact fuel quality and who can prove the cause.**

### Editor’s pre-publication refresh checklist

- Capture live Google Trends and note country/period/category/search type.
- Verify the exact E20 Janta Party handle and current metrics.
- Locate the official February 2026 notification text and any exceptions.
- Obtain a written OMC statement on pump labeling, E0/E10 availability and complaint sampling.
- Recheck manufacturer pages for Maruti, Hyundai, Kia, MG, Bajaj/KTM and pre-2023 models.
- Check the Raipur order/appeal status and use formal documents if discussing law.
- Update Kerala pump prices and film real labels.
- Do not reuse a removed/edited creator clip without rights and provenance.
`;

const manifest = `# E20 India Research Manifest

## Identification

- **Agent name:** ${agent}
- **Model name:** OpenAI GPT-5 family (Codex)
- **Model version:** Exact serving snapshot not exposed to the agent
- **Research mode used:** Multi-source web research; official-document review; manufacturer/manual review; news chronology reconstruction; social-sentiment sampling; structured fact-checking; spreadsheet artifact generation and visual QA
- **Research start time:** 2026-07-29, America/New_York (exact session-start timestamp not exposed)
- **Research completion time:** Pending final upload verification
- **Current-date cutoff:** ${date}

## Platforms searched

Government of India/PIB, MoPNG, NITI Aayog, BIS, Parliament-linked releases, manufacturer sites and owner manuals, SIAM, SAE, NREL, EPA, peer-reviewed databases, Google web search, Google Trends (attempted), YouTube/search-indexed video pages, national/automotive/legal news, Malayalam news, Reddit, and cross-platform reporting on X/Instagram campaigns.

## Search tools used

- Web search/opening for current public sources
- Direct document/page retrieval
- Google Drive destination metadata/listing
- Google Trends endpoint and in-app browser attempt
- Local artifact generation and spreadsheet render/inspection

## Sources reviewed

- **Curated source-library records:** ${sources.length}
- **Priority:** government/parliamentary/standards/manufacturer/research first; news for chronology; social sources for sentiment only
- **Deduplication:** canonical URLs retained once in the library

## Files created

1. ${filenames.dossier}
2. ${filenames.sources}
3. ${filenames.factCsv}
4. ${filenames.factXlsx}
5. ${filenames.videos}
6. ${filenames.manifest}

## Files successfully saved

Pending Google Drive upload and read-back verification. This section will be replaced with observed Drive links; no success is claimed in this draft.

## Google Drive destination

- **Required folder:** ParaCast/Workshop/E20/01-Research/
- **Folder ID:** 1f50ninfRbOGSTggIV1fDvyrncrWcr4b7
- **Direct folder:** https://drive.google.com/drive/folders/1f50ninfRbOGSTggIV1fDvyrncrWcr4b7
- **Pre-upload verification:** Destination metadata matched “01-Research,” parent ID matched the supplied E20 project folder, and the destination was empty. No pre-existing file was overwritten.

## Known limitations

1. **Google Trends:** Exact normalized values, breakout labels and state rankings were not captured. The endpoint returned HTTP 429 and the browser bridge could not initialize. The dossier reports only an event-dated qualitative pattern and instructs the editor to capture a fresh export.
2. **YouTube live metrics:** Direct live pages were not consistently accessible. Four search results exposed views/subscribers; other values are explicitly “not reliably visible.” No fake counts, durations, thumbnail text or velocity metrics were added.
3. **Social platforms:** X, Threads, Facebook and Instagram were analyzed mainly through reputable current reporting. Duplicate “E20 Janta Party” accounts make attribution and follower counts volatile.
4. **Station labeling/choice:** No single authoritative current circular covering every pump’s display requirement and E0/E10 alternative availability was located.
5. **Model-specific compatibility:** Several brands publish current-product positions but not complete legacy tables. A model year or BS stage was never used as sole proof.
6. **Legal status:** The Raipur order is a reported district consumer-commission decision and may be appealed; the formal order/appeal should be checked before a legal conclusion.
7. **Regional measurement:** No controlled Kerala-specific failure, humidity, mileage or search-rank dataset was found.
8. **Prices:** Petrol and feedstock economics change; illustrative ₹ calculations use clearly stated assumptions and are not current price quotes.

## Sources/platforms that could not be fully accessed

- Live Google Trends interactive data/export
- Fully rendered live YouTube watch pages and comment streams
- Complete authenticated X/Instagram/Threads/Facebook post and follower histories
- A direct primary manufacturer release for every legacy brand/model, especially MG, KTM and portions of Bajaj/Mahindra coverage
- Formal court transcript/order for the disputed Supreme Court quote
- The original stable URL/version of the altered/removed Sourav Joshi segment

## Claims requiring human verification before publication

1. Exact wording, exceptions and gazette number of the February 2026 nationwide E20/RON 95 notification.
2. Current pump-labeling obligation and OMC fuel-sampling procedure.
3. Live Google Trends pattern and Kerala geography.
4. Current identities/metrics of the two similarly named E20 Janta Party accounts.
5. Exact owner-manual/VIN answer for any vehicle shown on screen.
6. Direct OEM source for the broad Bajaj/KTM and Mahindra legacy claims.
7. Royal Enfield retrofit-kit model eligibility, availability, cost and warranty.
8. Raipur commission order text and appeal status.
9. Formal court record behind the disputed “ongoing experiment” quote.
10. Current pump prices and any E0/E10 specialty availability in Kerala.

## Confidence

- **Overall:** High for core chemistry, energy/mileage direction, policy history, current government position and major June–July chronology.
- **Moderate-high:** Brand portfolio statements and warranty principles.
- **Moderate:** Complete legacy-fleet coverage, legal outcomes, platform metrics and regional incidence.

## Unique contribution

This package treats E20 not as a binary “safe/unsafe” argument but as three linked evidence problems—material compatibility, formal certification and optimization—and then connects those to consumer proof, choice and cost per kilometre. It also reconstructs the current attention as a dated cascade rather than attributing it to a single viral post, and it explicitly refuses to turn inaccessible Trends/YouTube panels into fabricated numbers.
`;

await fs.writeFile(path.join(outputDir, filenames.dossier), dossier, "utf8");
await fs.writeFile(path.join(outputDir, filenames.sources), sourceLibrary, "utf8");

const factHeaders = ["Claim", "Verdict", "Explanation", "Evidence Quality", "Primary Source", "Secondary Source", "Important Caveat", "Last Verified Date", "Agent Name"];
await fs.writeFile(path.join(outputDir, filenames.factCsv), toCsv(factHeaders, factRows), "utf8");

const videoHeaders = ["Video Title", "Channel", "URL", "Upload Date", "Views", "Subscribers", "Views-to-Subscriber Ratio", "Duration", "Language", "Thumbnail Text", "Opening Hook", "Main Claim", "Evidence Used", "Missing Context", "Comment Themes", "Reaction Potential", "Recommendation", "Agent Name"];
await fs.writeFile(path.join(outputDir, filenames.videos), toCsv(videoHeaders, videoRows), "utf8");
await fs.writeFile(path.join(outputDir, filenames.manifest), manifest, "utf8");

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("Fact Check");
sheet.showGridLines = false;
sheet.getRange(`A1:I${factRows.length + 1}`).values = [factHeaders, ...factRows];
sheet.freezePanes.freezeRows(1);
sheet.getRange("A1:I1").format = {
  fill: "#0F4C5C",
  font: { bold: true, color: "#FFFFFF", size: 11 },
  verticalAlignment: "center",
  wrapText: true,
  borders: { preset: "all", style: "thin", color: "#D8E3E7" },
};
sheet.getRange(`A2:I${factRows.length + 1}`).format = {
  verticalAlignment: "top",
  wrapText: true,
  borders: { preset: "all", style: "thin", color: "#D8E3E7" },
};
sheet.getRange(`A2:A${factRows.length + 1}`).format.font = { bold: true, color: "#17324D" };
sheet.getRange(`B2:B${factRows.length + 1}`).format.font = { bold: true };
sheet.getRange(`H2:H${factRows.length + 1}`).setNumberFormat("yyyy-mm-dd");
sheet.getRange(`B2:B${factRows.length + 1}`).dataValidation = {
  rule: { type: "list", values: ["True", "Mostly true", "Partly true", "Misleading", "Unsupported", "False", "Insufficient evidence"] }
};
sheet.getRange(`B2:B${factRows.length + 1}`).conditionalFormats.add("containsText", {
  text: "False", format: { fill: "#FDE2E1", font: { color: "#9E2A2B", bold: true } }
});
sheet.getRange(`B2:B${factRows.length + 1}`).conditionalFormats.add("containsText", {
  text: "Misleading", format: { fill: "#FFF1CC", font: { color: "#7A4E00", bold: true } }
});
sheet.getRange(`B2:B${factRows.length + 1}`).conditionalFormats.add("containsText", {
  text: "Unsupported", format: { fill: "#FCE4EC", font: { color: "#8E2457", bold: true } }
});
sheet.getRange(`A1:I${factRows.length + 1}`).format.rowHeight = 72;
sheet.getRange("A1:I1").format.rowHeight = 36;
const widths = [34, 16, 62, 16, 25, 25, 50, 16, 14];
for (let i = 0; i < widths.length; i++) {
  sheet.getRangeByIndexes(0, i, factRows.length + 1, 1).format.columnWidth = widths[i];
}
const table = sheet.tables.add(`A1:I${factRows.length + 1}`, true, "E20FactCheckTable");
table.style = "TableStyleMedium2";
table.showFilterButton = true;
table.showBandedRows = true;

const preview = await workbook.render({
  sheetName: "Fact Check",
  range: "A1:I12",
  autoCrop: "all",
  scale: 0.8,
  format: "png",
});
await fs.writeFile(path.join(outputDir, "E20-Fact-Check-preview.png"), new Uint8Array(await preview.arrayBuffer()));

const inspectRegion = await workbook.inspect({
  kind: "region",
  sheetId: "Fact Check",
  range: "A1:I6",
  maxChars: 6000,
});
await fs.writeFile(path.join(outputDir, "fact-check-inspection.txt"), inspectRegion.ndjson ?? String(inspectRegion), "utf8");

const inspectFormulas = await workbook.inspect({
  kind: "formula",
  sheetId: "Fact Check",
  range: `A1:I${factRows.length + 1}`,
  maxChars: 2000,
  options: { maxResults: 50 },
});
await fs.writeFile(path.join(outputDir, "fact-check-formula-scan.txt"), inspectFormulas.ndjson ?? String(inspectFormulas), "utf8");

const xlsx = await SpreadsheetFile.exportXlsx(workbook);
const workbookPath = path.join(outputDir, filenames.factXlsx);
await xlsx.save(workbookPath);

// Read back the exported file so the validation covers the deliverable bytes,
// not only the in-memory workbook used to create them.
const savedBlob = await FileBlob.load(workbookPath);
const importedWorkbook = await SpreadsheetFile.importXlsx(savedBlob);
const importedCheck = await importedWorkbook.inspect({
  kind: "region",
  sheetId: "Fact Check",
  range: "A1:I6",
  maxChars: 6000,
});
await fs.writeFile(path.join(outputDir, "fact-check-readback.txt"), importedCheck.ndjson ?? String(importedCheck), "utf8");
const importedPreview = await importedWorkbook.render({
  sheetName: "Fact Check",
  range: "A1:I12",
  autoCrop: "all",
  scale: 0.8,
  format: "png",
});
await fs.writeFile(path.join(outputDir, "E20-Fact-Check-readback-preview.png"), new Uint8Array(await importedPreview.arrayBuffer()));

console.log(JSON.stringify({
  outputDir,
  filenames,
  sources: sources.length,
  factRows: factRows.length,
  videoRows: videoRows.length,
}, null, 2));
