using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Sarsa
{
    double alpha = 0.2f;
    double gamma = 0.99f;
    double epsilon = 0.01f;
    double tau = 0.4f;
    double t;

    const int EPSGREEDY = 11;
    const int SOFTMAX = 22;
    int actionSelector = EPSGREEDY;

    private double[][] Q;

    public Sarsa(double LearningRate, double DiscountRate, int[] s, int[] a)
    {
        alpha = LearningRate;
        gamma = DiscountRate;

        Q = new double[s.Length][];
        for (int i = 0; i < s.Length; i++) Q[i] = new double[a.Length];
        for (int i = 0; i < s.Length; i++)
        {
            for (int j = 0; j < a.Length; j++)
            {
                Q[i][j] = 1.0f;
            }
        }
    }

    public void ValueUpdate(int s, int a, int ss, int aa, double r)
    {
        Q[s][a] = Q[s][a] + alpha * (r + gamma * Q[ss][aa] - Q[s][a]);
    }

    public int SelectAction(int s)
    {
        int tardex = 0;

        switch (actionSelector)
        {
            case EPSGREEDY:
                tardex = epsilon_greedy_selection(Q[s]);
                break;
            case SOFTMAX:
                tardex = softmax_selection(Q[s]);
                break;
        }

        return tardex;
    }

    public int epsilon_greedy_selection(double[] values)
    {
        double[] p = { epsilon, 1.0f - epsilon };

        if (selection_from_dispersion(p) == 0)
        {
            return random_selection(values);
        }
        else
        {
            return max_selection(values);
        }
    }

    public int softmax_selection(double[] values)
    {
        int values_num = values.Length;
        double[] probability = new double[values_num];
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

        return selection_from_dispersion(probability);
    }

    //与えられた確率分布の通りにインデックスを選択する
    public int selection_from_dispersion(double[] probability)
    {
        int values_num = probability.Length;
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

    public int random_selection(double[] values)
    {
        int values_num = values.Length;
        System.Random r = new System.Random();
        double rand = r.NextDouble();
        int tardex = (int)((rand * 1000) % values_num);

        return tardex;
    }

    public int max_selection(double[] values)
    {
        int values_num = values.Length;
        int maxdex = 0;

        for (int i = 1; i < values_num; i++)
        {
            if (values[i] > values[maxdex]) maxdex = i;
        }

        return maxdex;
    }

    public void EpsilonSet(double eps)
    {
        epsilon = eps;
    }
    public void AlphaSet(double alp)
    {
        alpha = alp;
    }
    public void GammaSet(double gam)
    {
        gamma = gam;
    }
    public void TauSet(double t)
    {
        tau = t;
    }
}
