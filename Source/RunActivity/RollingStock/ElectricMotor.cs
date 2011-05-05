﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS;

namespace ORTS
{
    public class ElectricMotor
    {
        protected float developedTorqueNm;
        public float DevelopedTorqueNm { get { return developedTorqueNm; } }

        protected float loadTorqueNm;
        public float LoadTorqueNm { set { loadTorqueNm = value; } get { return loadTorqueNm; } }

        protected float frictionTorqueNm;
        public float FrictionTorqueNm { set { frictionTorqueNm = Math.Abs(value); } get { return frictionTorqueNm; } }

        float inertiaKgm2;
        public float InertiaKgm2
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Inertia must be greater than 0");
                inertiaKgm2 = value;
            }
            get
            {
                return inertiaKgm2; 
            }
        }

        protected float revolutionsRad;
        public float RevolutionsRad { get { return revolutionsRad; } set { revolutionsRad = value; } }

        protected float temperatureK;
        public float TemperatureK { get { return temperatureK; } }

        Integrator tempIntegrator = new Integrator();

        public float ThermalCoeffJ_m2sC { set; get; }
        public float SpecificHeatCapacityJ_kg_C { set; get; }
        public float SurfaceM { set; get; }
        public float WeightKg { set; get; }

        protected float powerLossesW;

        public float CoolingPowerW { set; get; }

        float transmitionRatio;
        public float TransmitionRatio
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Transmition ratio must be greater than zero");
                transmitionRatio = value;
            }
            get
            {
                return transmitionRatio;
            }
        }

        float axleDiameterM;
        public float AxleDiameterM
        {
            set
            {
                if (value <= 0.0)
                    throw new NotSupportedException("Axle diameter must be greater than zero");
                axleDiameterM = value;
            }
            get
            {
                return axleDiameterM;
            }
        }

        public Axle AxleConnected;

        public ElectricMotor()
        {
            developedTorqueNm = 0.0f;
            loadTorqueNm = 0.0f;
            inertiaKgm2 = 1.0f;
            revolutionsRad = 0.0f;
            axleDiameterM = 1.0f;
            transmitionRatio = 1.0f;
            temperatureK = 0.0f;
            ThermalCoeffJ_m2sC = 50.0f;
            SpecificHeatCapacityJ_kg_C = 40.0f;
            SurfaceM = 2.0f;
            WeightKg = 5.0f;
        }

        public virtual void Update(float timeSpan)
        {
            //revolutionsRad += timeSpan / inertiaKgm2 * (developedTorqueNm + loadTorqueNm + (revolutionsRad == 0.0 ? 0.0 : frictionTorqueNm));
            //if (revolutionsRad < 0.0)
            //    revolutionsRad = 0.0;
            temperatureK = tempIntegrator.Integrate(timeSpan, 1.0f/(SpecificHeatCapacityJ_kg_C * WeightKg)*((powerLossesW - CoolingPowerW) / (ThermalCoeffJ_m2sC * SurfaceM) - temperatureK));

        }

        public virtual void Reset()
        {
            revolutionsRad = 0.0f;
        }
    }
}