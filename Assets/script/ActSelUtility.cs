using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ActSelUtility
{
    public const int GREEDY    = 0;
    public const int RANDOM    = 1;
    public const int EPSGREEDY = 2;
    public const int SOFTMAX   = 3;
    

    private double epsilon = 0.1f;
    private double tau = 0.4f;

    public ActSelUtility(){}

    public ActSelUtility(double Epsilon, double Tau)
    {
        epsilon = Epsilon;
        tau = Tau;
    }

    public int greedy_selection(List<double> values)
    {
        int values_num = values.Count;
        int maxdex = 0;

        for (int i = 1; i < values_num; i++)
        {
            if (values[i] > values[maxdex]) maxdex = i;
        }

        return maxdex;
    }

    public int random_selection(List<double> values)
    {
        int values_num = values.Count;
        System.Random r = new System.Random();
        double rand = r.NextDouble();
        int tardex = (int)((rand * 1000) % values_num);

        return tardex;
    }

    public int epsilon_greedy_selection(List<double> values)
    {
        List<double> p = new List<double>() { epsilon, 1.0f - epsilon };

        if (selection_from_dispersion( p) == 0)
        {
            return random_selection( values);
        }
        else
        {
            return greedy_selection( values);
        }
    }

    public int softmax_selection( List<double> values)
    {
        int values_num = values.Count;
        List<double> probability = new List<double>(new double[values_num]);
        double probabilitySum = 0.0f;

        for (int i = 0; i < values_num; i++)
        {
            probability[i] = Math.Pow(Math.E, values[i] / tau);
            probabilitySum += probability[i];
        }

        for (int i = 0; i < values_num; i++)
        {
            probability[i] = probability[i] / probabilitySum;
        }

        return selection_from_dispersion( probability);
    }

    //与えられた確率分布の通りにインデックスを選択する
    public int selection_from_dispersion( List<double> probability)
    {
        int values_num = probability.Count;
        int tardex = 0;
        System.Random r = new System.Random();
        double rand = r.NextDouble();
        double[] pSum = new double[values_num];

        pSum[0] = probability[0];
        for (int i = 1; i < values_num; i++)
        {
            pSum[i] = probability[i] + pSum[i - 1];
        }

        for (int i = 0; i < values_num; i++)
        {
            if (rand <= pSum[i])
            {
                tardex = i;
                break;
            }
        }

        return tardex;
    }
}