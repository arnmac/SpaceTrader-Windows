/*******************************************************************************
 *
 * Space Trader for Windows 1.3.0
 *
 * Copyright (C) 2004 Jay French, All Rights Reserved
 *
 * Original coding by Pieter Spronck, Sam Anderson, Samuel Goldstein, Matt Lee
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation; either version 2 of the License, or (at your option) any
 * later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * If you'd like a copy of the GNU General Public License, go to
 * http://www.gnu.org/copyleft/gpl.html.
 * 
 * You can contact the author at spacetrader@frenchfryz.com
 *
 ******************************************************************************/
using System;
using System.Collections;

namespace Fryz.Apps.SpaceTrader
{
	[Serializable()]      
	public class Ship : ShipSpec
	{
		#region Member Declarations

		private int						_fuel;
		private int						_hull;
		private int						_tribbles				= 0;
		private int[]					_cargo					= new int[10];
		private Weapon[]			_weapons;
		private Shield[]			_shields;
		private Gadget[]			_gadgets;
		private CrewMember[]	_crew;
		private bool					_pod						= false;

		private bool[]				_tradeableItems;

		#endregion

		#region Methods

		public Ship(ShipType type)
		{
			SetValues(type);
		}

		public Ship(OpponentType oppType)
		{
			if (oppType == OpponentType.FamousCaptain)
			{
				SetValues(Consts.ShipSpecs[Consts.MaxShip].Type);

				for (int i = 0; i < Shields.Length; i++)
					AddEquipment(Consts.Shields[(int)ShieldType.Reflective]);

				for (int i = 0; i < Weapons.Length; i++)
					AddEquipment(Consts.Weapons[(int)WeaponType.MilitaryLaser]);

				AddEquipment(Consts.Gadgets[(int)GadgetType.NavigatingSystem]);
				AddEquipment(Consts.Gadgets[(int)GadgetType.TargetingSystem]);

				Crew[0] = Game.CurrentGame.Mercenaries[(int)CrewMemberId.FamousCaptain];
			}
			else if (oppType == OpponentType.Bottle)
			{
				SetValues(ShipType.Bottle);
			}
			else
			{
				int	tries	= oppType == OpponentType.Mantis ? (int)Game.CurrentGame.Difficulty + 1 :
										Math.Max(1, Game.CurrentGame.Commander.Worth / 150000 + (int)Game.CurrentGame.Difficulty -
										(int)Difficulty.Normal);

				GenerateOpponentShip(oppType);
				GenerateOpponentAddCrew();
				GenerateOpponentAddGadgets(tries);
				GenerateOpponentAddShields(tries);
				GenerateOpponentAddWeapons(tries);

				if (oppType != OpponentType.Mantis)
					GenerateOpponentSetHullStrength();

				if (oppType != OpponentType.Police)
					GenerateOpponentAddCargo(oppType == OpponentType.Pirate);
			}
		}

		protected override void SetValues(ShipType type)
		{
			base.SetValues(type);

			_weapons	= new Weapon[WeaponSlots];
			_shields	= new Shield[ShieldSlots];
			_gadgets	= new Gadget[GadgetSlots];
			_crew			= new CrewMember[CrewQuarters];
			_fuel			= FuelTanks;
			_hull			= HullStrength;
		}

		public void AddEquipment(Equipment item)
		{
			Equipment[] equip	= EquipmentByType(item.EquipmentType);

			int					slot	= -1;
			for (int i = 0; i < equip.Length && slot == -1; i++)
				if (equip[i] == null)
					slot	= i;

			equip[slot]	= item.Clone();
		}

		public int BaseWorth(bool forInsurance)
		{
			int i;
			int price	=
				// Trade-in value is three-fourths the original price
				Price * (Tribbles > 0 && !forInsurance ? 1 : 3) / 4
				// subtract repair costs
				- (HullStrength - Hull) * RepairCost
				// subtract costs to fill tank with fuel
				- (FuelTanks - Fuel) * FuelCost;
			// Add 3/4 of the price of each item of equipment
			for (i = 0; i < _weapons.Length; i++)
				if (_weapons[i] != null)
					price += _weapons[i].SellPrice;
			for (i = 0; i < _shields.Length; i++)
				if (_shields[i] != null)
					price += _shields[i].SellPrice;
			for (i = 0; i < _gadgets.Length; i++)
				if (_gadgets[i] != null)
					price += _gadgets[i].SellPrice;

			return price;
		}

		public int Bounty()
		{
			int	price	= Price;

			for (int i = 0; i < Weapons.Length; i++)
				if (Weapons[i] != null)
					price	+= Weapons[i].Price;

			for (int i = 0; i < Shields.Length; i++)
				if (Shields[i] != null)
					price	+= Shields[i].Price;

			// Gadgets aren't counted in the price, because they are already taken into account in
			// the skill adjustment of the price.

			price = price * (2 * Pilot + Engineer + 3 * Fighter) / 60;

			// Divide by 200 to get the bounty, then round down to the nearest 25.
			int	bounty	= price / 200 / 25 * 25;

			return Math.Max(25, Math.Min(2500, bounty));
		}

		public Equipment[] EquipmentByType(EquipmentType type)
		{
			Equipment[]	equip	= null;
			switch (type)
			{
				case EquipmentType.Weapon:
					equip	= Weapons;
					break;
				case EquipmentType.Shield:
					equip	= Shields;
					break;
				case EquipmentType.Gadget:
					equip	= Gadgets;
					break;
			}
			return equip;
		}

		public void Fire(int crewId)
		{
			int	skill			= Trader;

			if (crewId < Crew.Length - 1)
				Crew[crewId]	= Crew[crewId + 1];
			Crew[Crew.Length - 1]	= null;

			if (Trader != skill)
				Game.CurrentGame.RecalculateBuyPrices(Game.CurrentGame.Commander.CurrentSystem);
		}

		private void GenerateOpponentAddCargo(bool pirate)
		{
			if (CargoBays > 0)
			{
				Difficulty	diff				= Game.CurrentGame.Difficulty;
				int					baysToFill	= CargoBays;

				if (diff >= Difficulty.Normal)
					baysToFill	= Math.Min(15, 3 + Functions.GetRandom(baysToFill - 5));

				if (pirate)
				{
					if (diff < Difficulty.Normal)
						baysToFill	= baysToFill * 4 / 5;
					else
						baysToFill	= Math.Max(1, baysToFill / (int)diff);
				}

				for (int bays, i = 0; i < baysToFill; i += bays)
				{
					int	item		 = Functions.GetRandom(Consts.TradeItems.Length);
					bays				 = Math.Min(baysToFill - i, 1 + Functions.GetRandom(10 - item));
					Cargo[item]	+= bays;
				}
			}
		}

		private void GenerateOpponentAddCrew()
		{
			CrewMember[]	mercs	= Game.CurrentGame.Mercenaries;
			Difficulty		diff	= Game.CurrentGame.Difficulty;

			Crew[0]							= mercs[(int)CrewMemberId.Opponent];
			Crew[0].Pilot				= 1 + Functions.GetRandom(Consts.MaxSkill);
			Crew[0].Fighter			= 1 + Functions.GetRandom(Consts.MaxSkill);
			Crew[0].Trader			= 1 + Functions.GetRandom(Consts.MaxSkill);

			if (Game.CurrentGame.WarpSystem.Id == StarSystemId.Kravat && WildOnBoard && Functions.GetRandom(10) < (int)diff + 1)
				Crew[0].Engineer	= Consts.MaxSkill;
			else
				Crew[0].Engineer	= 1 + Functions.GetRandom(Consts.MaxSkill);

			int	numCrew	= 0;
			if (diff == Difficulty.Impossible)
				numCrew	= CrewQuarters;
			else
			{
				numCrew	= 1 + Functions.GetRandom(CrewQuarters);
				if (diff == Difficulty.Hard && numCrew < CrewQuarters)
					numCrew++;
			}

			for (int i = 1; i < numCrew; i++)
				Crew[i]	= mercs[Functions.GetRandom((int)CrewMemberId.Zeethibal)];
		}

		private void GenerateOpponentAddGadgets(int tries)
		{
			if (GadgetSlots > 0)
			{
				int	numGadgets	= 0;

				if (Game.CurrentGame.Difficulty == Difficulty.Impossible)
					numGadgets	= GadgetSlots;
				else
				{
					numGadgets	= Functions.GetRandom(GadgetSlots + 1);
					if (numGadgets < GadgetSlots && (tries > 4 || (tries > 2 && Functions.GetRandom(2) > 0)))
						numGadgets++;
				}

				for (int i = 0; i < numGadgets; i++)
				{
					int bestGadgetType	= 0;
					for (int j = 0; j < tries; j++)
					{
						int x						= Functions.GetRandom(100);
						int	sum					= Consts.Gadgets[0].Chance;
						int	gadgetType	= 0;
						while (sum < x && gadgetType >= Consts.Gadgets.Length - 1)
						{
							gadgetType++;
							sum	+= Consts.Gadgets[gadgetType].Chance;
						}
						if (!HasGadget(Consts.Gadgets[gadgetType].Type) && gadgetType > bestGadgetType)
							bestGadgetType = gadgetType;
					}

					AddEquipment(Consts.Gadgets[bestGadgetType]);
				}
			}
		}

		private void GenerateOpponentAddShields(int tries)
		{
			if (ShieldSlots > 0)
			{
				int	numShields	= 0;

				if (Game.CurrentGame.Difficulty == Difficulty.Impossible)
					numShields	= ShieldSlots;
				else
				{
					numShields	= Functions.GetRandom(ShieldSlots + 1);
					if (numShields < ShieldSlots && (tries > 3 || (tries > 1 && Functions.GetRandom(2) > 0)))
						numShields++;
				}

				for (int i = 0; i < numShields; i++)
				{
					int bestShieldType	= 0;
					for (int j = 0; j < tries; j++)
					{
						int x						= Functions.GetRandom(100);
						int	sum					= Consts.Shields[0].Chance;
						int	shieldType	= 0;
						while (sum < x && shieldType >= Consts.Shields.Length - 1)
						{
							shieldType++;
							sum	+= Consts.Shields[shieldType].Chance;
						}
						if (!HasShield(Consts.Shields[shieldType].Type) && shieldType > bestShieldType)
							bestShieldType = shieldType;
					}

					AddEquipment(Consts.Shields[bestShieldType]);

					Shields[i].Charge	= 0;
					for (int j = 0; j < 5; j++)
					{
						int charge	= 1 + Functions.GetRandom(Shields[i].Power);
						if (charge > Shields[i].Charge)
							Shields[i].Charge	= charge;
					}
				}
			}
		}

		private void GenerateOpponentAddWeapons(int tries)
		{
			if (WeaponSlots > 0)
			{
				int	numWeapons	= 0;

				if (Game.CurrentGame.Difficulty == Difficulty.Impossible)
					numWeapons	= WeaponSlots;
				else if (WeaponSlots == 1)
					numWeapons	= 1;
				else
				{
					numWeapons	= 1 + Functions.GetRandom(WeaponSlots);
					if (numWeapons < WeaponSlots && (tries > 4 || (tries > 3 && Functions.GetRandom(2) > 0)))
						numWeapons++;
				}

				for (int i = 0; i < numWeapons; i++)
				{
					int bestWeaponType	= 0;
					for (int j = 0; j < tries; j++)
					{
						int x						= Functions.GetRandom(100);
						int	sum					= Consts.Weapons[0].Chance;
						int	weaponType	= 0;
						while (sum < x && weaponType >= Consts.Weapons.Length - 1)
						{
							weaponType++;
							sum	+= Consts.Weapons[weaponType].Chance;
						}
						if (!HasWeapon(Consts.Weapons[weaponType].Type, true) && weaponType > bestWeaponType)
							bestWeaponType = weaponType;
					}

					AddEquipment(Consts.Weapons[bestWeaponType]);
				}
			}
		}

		private void GenerateOpponentSetHullStrength()
		{
			// If there are shields, the hull will probably be stronger
			if (ShieldStrength == 0 || Functions.GetRandom(5) == 0)
			{
				Hull	= 0;

				for (int i = 0; i < 5; i++)
				{
					int hull	= 1 + Functions.GetRandom(HullStrength);
					if (hull > Hull)
						Hull	= hull;
				}
			}
		}

		private void GenerateOpponentShip(OpponentType oppType)
		{
			Commander				cmdr		= Game.CurrentGame.Commander;
			PoliticalSystem	polSys	= Game.CurrentGame.WarpSystem.PoliticalSystem;

			if (oppType == OpponentType.Mantis)
				SetValues(ShipType.Mantis);
			else
			{
				ShipType	oppShipType;
				int				tries				= 1;

				switch (oppType)
				{
					case OpponentType.Pirate:
						// Pirates become better when you get richer
						tries	= 1 + cmdr.Worth / 100000;
						tries	= Math.Max(1, tries + (int)Game.CurrentGame.Difficulty - (int)Difficulty.Normal);
						break;
					case OpponentType.Police:
						// The police will try to hunt you down with better ships if you are 
						// a villain, and they will try even harder when you are considered to
						// be a psychopath (or are transporting Jonathan Wild)
						if (cmdr.PoliceRecordScore < Consts.PoliceRecordScorePsychopath || WildOnBoard)
							tries	= 5;
						else if (cmdr.PoliceRecordScore < Consts.PoliceRecordScoreVillain)
							tries	= 3;
						else
							tries	= 1;
						tries	= Math.Max(1, tries + (int)Game.CurrentGame.Difficulty - (int)Difficulty.Normal);
						break;
				}

				if (oppType == OpponentType.Trader)
					oppShipType	= ShipType.Flea;
				else
					oppShipType	= ShipType.Gnat;

				int	total	= 0;
				for (int i = 0; i < Consts.MaxShip; i++)
				{
					ShipSpec	spec	= Consts.ShipSpecs[i];
					if (polSys.ShipTypeLikely(spec.Type, oppType))
						total	+= spec.Occurance;
				}

				for (int i = 0; i < tries; i++)
				{
					int	x		= Functions.GetRandom(total);
					int	sum	= -1;
					int	j		= -1;

					do
					{
						j++;
						if (polSys.ShipTypeLikely(Consts.ShipSpecs[j].Type, oppType))
						{
							if (sum > 0)
								sum	+= Consts.ShipSpecs[j].Occurance;
							else
								sum	 = Consts.ShipSpecs[j].Occurance;
						}
					} while (sum < x && j < Consts.MaxShip);

					if (j > (int)oppShipType)
						oppShipType	= Consts.ShipSpecs[j].Type;
				}

				SetValues(oppShipType);
			}
		}

		// *************************************************************************
		// Returns the index of a trade good that is on a given ship that can be
		// bought/sold in the current system.
		// JAF - Made this MUCH simpler by storing an array of booleans indicating
		// the tradeable goods when HasTradeableItem is called.
		// *************************************************************************
		public int GetRandomTradeableItem()
		{
			int	index	= Functions.GetRandom(TradeableItems.Length);

			while (!TradeableItems[index])
				index	= (index + 1) % TradeableItems.Length;

			return index;
		}

		public bool HasCrew(CrewMemberId id)
		{
			bool found	= false;
			for (int i = 0; i < Crew.Length && !found; i++)
			{
				if (Crew[i] != null && Crew[i].Id == id)
					found	= true;
			}
			return found;
		}

		public bool HasEquipment(Equipment item)
		{
			bool found	= false;
			switch (item.EquipmentType)
			{
				case EquipmentType.Weapon:
					found	= HasWeapon(((Weapon)item).Type, true);
					break;
				case EquipmentType.Shield:
					found	= HasShield(((Shield)item).Type);
					break;
				case EquipmentType.Gadget:
					found	= HasGadget(((Gadget)item).Type);
					break;
			}
			return found;
		}

		public bool HasGadget(GadgetType gadgetType)
		{
			bool found	= false;
			for (int i = 0; i < Gadgets.Length && !found; i++)
			{
				if (Gadgets[i] != null && Gadgets[i].Type == gadgetType)
					found	= true;
			}
			return found;
		}

		public bool HasShield(ShieldType shieldType)
		{
			bool found	= false;
			for (int i = 0; i < Shields.Length && !found; i++)
			{
				if (Shields[i] != null && Shields[i].Type == shieldType)
					found	= true;
			}
			return found;
		}

		// *************************************************************************
		// Determines if a given ship is carrying items that can be bought or sold
		// in the current system.
		// *************************************************************************
		public bool HasTradeableItems()
		{
			bool	found			= false;
			bool	criminal	= Game.CurrentGame.Commander.PoliceRecordScore <
				Consts.PoliceRecordScoreDubious;
			_tradeableItems	= new bool[10];

			for (int i = 0; i < Cargo.Length; i++)
			{
				// Trade only if trader is selling and the item has a buy price on the
				// local system, or trader is buying, and there is a sell price on the
				// local system.
				// Criminals can only buy or sell illegal goods, Noncriminals cannot buy
				// or sell such items.
				// Simplified this - JAF
				if (Cargo[i] > 0 && !(criminal ^ Consts.TradeItems[i].Illegal) && (
					(!CommandersShip && Game.CurrentGame.PriceCargoBuy[i] > 0) ||
					(CommandersShip && Game.CurrentGame.PriceCargoSell[i] > 0)))
				{
					found							= true;
					TradeableItems[i]	= true;
				}
			}

			return found;
		}

		public bool HasWeapon(WeaponType weaponType, bool exactCompare)
		{
			bool found	= false;
			for (int i = 0; i < Weapons.Length && !found; i++)
			{
				if (Weapons[i] != null && (Weapons[i].Type == weaponType || !exactCompare && Weapons[i].Type > weaponType))
					found	= true;
			}
			return found;
		}

		public void Hire(CrewMember merc)
		{
			int	skill	= Trader;

			Crew[Crew[1] == null ? 1 : 2]	= merc;

			if (Trader != skill)
				Game.CurrentGame.RecalculateBuyPrices(Game.CurrentGame.Commander.CurrentSystem);
		}

		public void PerformRepairs()
		{
			// Engineer may do some repairs
			int repairs	 = Functions.GetRandom(Engineer) / 2;
			if (repairs > 0)
			{
				int used	 = Math.Min(repairs, HullStrength - Hull);
				Hull			+= used;
				repairs		-= used;
			}

			// Shields are easier to repair
			if (repairs > 0)
			{
				repairs	*= 2;

				for (int i = 0; i < Shields.Length && repairs > 0; i++)
				{
					if (Shields[i] != null)
					{
						int used					 = Math.Min(repairs, Shields[i].Power - Shields[i].Charge);
						Shields[i].Charge	+= used;
						repairs						-= used;
					}
				}
			}
		}

		public void RemoveEquipment(EquipmentType type, int slot)
		{
			Equipment[]	equip	= EquipmentByType(type);

			int					last	= equip.Length - 1;
			for (int i = slot; i < last; i++)
				equip[i]	= equip[i + 1];
			equip[last]	= null;
		}

		public void RemoveEquipment(EquipmentType type, object subType)
		{
			bool				found	= false;
			Equipment[]	equip	= EquipmentByType(type);

			for (int i = 0; i < equip.Length && !found; i++)
			{
				if (equip[i] != null && equip[i].TypeEquals(subType))
				{
					RemoveEquipment(type, i);
					found	= true;
				}
			}
		}

		public int WeaponStrength()
		{
			return WeaponStrength(WeaponType.PulseLaser, WeaponType.MorgansLaser);
		}

		public int WeaponStrength(WeaponType min, WeaponType max)
		{
			int	total	= 0;

			for (int i = 0; i < Weapons.Length; i++)
				if (Weapons[i] != null && Weapons[i].Type >= min && Weapons[i].Type <= max)
					total	+= Weapons[i].Power;

			return total;
		}

		public int Worth(bool forInsurance)
		{
			int price	= BaseWorth(forInsurance);
			for (int i = 0; i < _cargo.Length; i++)
				price	+= Game.CurrentGame.Commander.PriceCargo[i];

			return price;
		}

		#endregion

		#region Properties

		public int Fuel
		{
			get
			{
				return _fuel;
			}
			set
			{
				_fuel	= value;
			}
		}

		public int Hull
		{
			get
			{
				return _hull;
			}
			set
			{
				_hull	= value;
			}
		}

		public int Tribbles
		{
			get
			{
				return _tribbles;
			}
			set
			{
				_tribbles	= value;
			}
		}

		public int[] Cargo
		{
			get
			{
				return _cargo;
			}
		}

		public Weapon[] Weapons
		{
			get
			{
				return _weapons;
			}
		}

		public Shield[] Shields
		{
			get
			{
				return _shields;
			}
		}

		public Gadget[] Gadgets
		{
			get
			{
				return _gadgets;
			}
		}

		public CrewMember[] Crew
		{
			get
			{
				return _crew;
			}
		}

		public int CrewCount
		{
			get
			{
				int	total	= 0;
				for (int i = 0; i < Crew.Length; i++)
					if (Crew[i] != null)
						total++;
				return total;
			}
		}

		public bool EscapePod
		{
			get
			{
				return _pod;
			}
			set
			{
				_pod	= value;
			}
		}

		public bool[] TradeableItems
		{
			get
			{
				return _tradeableItems;
			}
		}

		public bool ArtifactOnBoard
		{
			get
			{
				return CommandersShip && Game.CurrentGame.QuestStatusArtifact == SpecialEvent.StatusArtifactOnBoard;
			}
		}

		// Changed the semantics of Filled versus total Cargo Bays.  Bays used for
		// transporting special items are now included in the total bays and in the
		// filled bays.  JAF
		public override int CargoBays
		{
			get
			{
				int	bays	= base.CargoBays;

				for (int i = 0; i < Gadgets.Length; i++)
					if (Gadgets[i] != null && Gadgets[i].Type == GadgetType.ExtraCargoBays)
						bays	+= 5;

				return bays;
			}
		}

		public bool Cloaked
		{
			get
			{
				int oppEng	= CommandersShip ? Game.CurrentGame.Opponent.Engineer : Game.CurrentGame.Commander.Ship.Engineer;
				return HasGadget(GadgetType.CloakingDevice) && Engineer > oppEng;
			}
		}

		public bool CommandersShip
		{
			get
			{
				return this == Game.CurrentGame.Commander.Ship;
			}
		}

		public int Engineer
		{
			get
			{
				int max	= 0;

				for (int i = 0; i < Crew.Length; i++)
				{
					if (Crew[i] != null && Crew[i].Engineer > max)
						max	= Crew[i].Engineer;
				}

				if (HasGadget(GadgetType.AutoRepairSystem))
					max	+= Consts.SkillBonus;

				return Functions.AdjustSkillForDifficulty(max);
			}
		}

		public Equipment[] Equipment
		{
			get
			{
				Equipment[]	equip	= new Equipment[9];
				int					i;

				for (i = 0; i < Weapons.Length; i++)
					equip[i]			= Weapons[i];
				for (i = 0; i < Shields.Length; i++)
					equip[3 + i]	= Shields[i];
				for (i = 0; i < Gadgets.Length; i++)
					equip[6 + i]	= Gadgets[i];

				return equip;
			}
		}

		public int Fighter
		{
			get
			{
				int max	= 0;

				for (int i = 0; i < Crew.Length; i++)
				{
					if (Crew[i] != null && Crew[i].Fighter > max)
						max	= Crew[i].Fighter;
				}

				if (HasGadget(GadgetType.TargetingSystem))
					max	+= Consts.SkillBonus;

				return Functions.AdjustSkillForDifficulty(max);
			}
		}

		// Changed the semantics of Filled versus total Cargo Bays.  Bays used for
		// transporting special items are now included in the total bays and in the
		// filled bays.  JAF
		public int FilledCargoBays
		{
			get
			{
				int	filled	= FilledNormalCargoBays;

				if (CommandersShip && Game.CurrentGame.QuestStatusJapori == SpecialEvent.StatusJaporiInTransit)
					filled	+= 10;

				if (ReactorOnBoard)
					filled	+= 5 + 10 - (Game.CurrentGame.QuestStatusReactor - 1) / 2;

				return filled;
			}
		}

		public int FilledNormalCargoBays
		{
			get
			{
				int	filled	= 0;

				for (int i = 0; i < _cargo.Length; i++)
					filled	+= _cargo[i];

				return filled;
			}
		}

		public int FreeCargoBays
		{
			get
			{
				return CargoBays - FilledCargoBays;
			}
		}

		public int FreeCrewQuarters
		{
			get
			{
				int count	= 0;
				for (int i = 0; i < Crew.Length; i++)
				{
					if (Crew[i] == null)
						count++;
				}
				return count;
			}
		}

		public int FreeSlots
		{
			get
			{
				return FreeSlotsGadget + FreeSlotsShield + FreeSlotsWeapon;
			}
		}

		public int FreeSlotsGadget
		{
			get
			{
				int count	= 0;
				for (int i = 0; i < Gadgets.Length; i++)
				{
					if (Gadgets[i] == null)
						count++;
				}
				return count;
			}
		}

		public int FreeSlotsShield
		{
			get
			{
				int count	= 0;
				for (int i = 0; i < Shields.Length; i++)
				{
					if (Shields[i] == null)
						count++;
				}
				return count;
			}
		}

		public int FreeSlotsWeapon
		{
			get
			{
				int count	= 0;
				for (int i = 0; i < Weapons.Length; i++)
				{
					if (Weapons[i] == null)
						count++;
				}
				return count;
			}
		}

		public override int FuelTanks
		{
			get
			{
				if (HasGadget(GadgetType.FuelCompactor))
					return Consts.MaxFuelTanks;
				else
					return base.FuelTanks;
			}
		}

		public bool JarekOnBoard
		{
			get
			{
				return CommandersShip && Game.CurrentGame.QuestStatusJarek > SpecialEvent.StatusJarekNotStarted &&
					Game.CurrentGame.QuestStatusJarek < SpecialEvent.StatusJarekDone;
			}
		}

		public int Pilot
		{
			get
			{
				int max	= 0;

				for (int i = 0; i < Crew.Length; i++)
				{
					if (Crew[i] != null && Crew[i].Pilot > max)
						max	= Crew[i].Pilot;
				}

				if (HasGadget(GadgetType.NavigatingSystem))
					max	+= Consts.SkillBonus;

				if (HasGadget(GadgetType.CloakingDevice))
					max	+= Consts.CloakBonus;

				return Functions.AdjustSkillForDifficulty(max);
			}
		}

		public bool ReactorOnBoard
		{
			get
			{
				int	status	= Game.CurrentGame.QuestStatusReactor;
				return CommandersShip && status > SpecialEvent.StatusReactorNotStarted &&
					status < SpecialEvent.StatusReactorDelivered;
			}
		}

		public int ShieldCharge
		{
			get
			{
				int	total	= 0;

				for (int i = 0; i < Shields.Length; i++)
					if (Shields[i] != null)
						total	+= Shields[i].Charge;

				return total;
			}
		}

		public int ShieldStrength
		{
			get
			{
				int	total	= 0;

				for (int i = 0; i < Shields.Length; i++)
					if (Shields[i] != null)
						total	+= Shields[i].Power;

				return total;
			}
		}

		// Crew members that are not hired/fired - Commander, Jarek, and Wild - JAF
		public CrewMember[]	SpecialCrew
		{
			get
			{
				ArrayList	list	= new ArrayList();
				for (int i = 0; i < Crew.Length; i++)
				{
					if (Crew[i] != null && (Crew[i].Id == CrewMemberId.Commander || Crew[i].Id > CrewMemberId.Zeethibal))
						list.Add(Crew[i]);
				}

				CrewMember[]	crew	= new CrewMember[list.Count];
				for (int i = 0; i < crew.Length; i++)
					crew[i]	= (CrewMember)list[i];

				return crew;
			}
		}

		public int Trader
		{
			get
			{
				int max	= 0;

				for (int i = 0; i < Crew.Length; i++)
				{
					if (Crew[i] != null && Crew[i].Trader > max)
						max	= Crew[i].Trader;
				}

				if (Game.CurrentGame.QuestStatusJarek == SpecialEvent.StatusJarekDone)
					max++;

				return Functions.AdjustSkillForDifficulty(max);
			}
		}

		public bool WildOnBoard
		{
			get
			{
				return CommandersShip && Game.CurrentGame.QuestStatusWild > SpecialEvent.StatusWildNotStarted &&
					Game.CurrentGame.QuestStatusWild < SpecialEvent.StatusWildDone;
			}
		}

		#endregion
	}
}
