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

namespace Fryz.Apps.SpaceTrader
{
	[Serializable()]      
	public class StarSystem
	{
		#region Member Declarations

		private StarSystemId		_id;
		private int							_x;
		private int							_y;
		private Size						_size;
		private TechLevel				_techLevel;
		private PoliticalSystem	_politicalSystem;
		private SystemPressure	_pressure;
		private SpecialResource	_specialResource;
		private SpecialEvent		_specialEvent			= null;
		private int[]						_tradeItems				= new int[10];
		private int							_countDown				= 0;
		private bool						_visited					= false;

		#endregion

		#region Methods

		public StarSystem(StarSystemId id, int x, int y, Size size, TechLevel techLevel, PoliticalSystem politicalSystem,
			SystemPressure pressure, SpecialResource specialResource)
		{
			_id								= id;
			_x								= x;
			_y								= y;
			_size							= size;
			_techLevel				= techLevel;
			_politicalSystem	= politicalSystem;
			_pressure					= pressure;
			_specialResource	= specialResource;
		}

		#endregion
	}
}
